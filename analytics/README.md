# Analytics with ADF and Databricks

The [templates](../deploy/templates) folder includes a [template for Azure Data Factory](../deploy/templates/azuredeploy-adf.json). This template will deploy an Azure Data Factory instances, which can be used to export specific resource types to [ndjson](http://ndjson.org/) files. Specifically, the template has an array parameter called `resourceTypes`, which can be set to the resource types for which export pipelines should be deployed.

Once deployed these export pipelines can be triggered manually to export a given resource type to blob storage. The ndjson files are easily consumed, e.g. in [Databricks](https://azure.microsoft.com/en-us/services/databricks/) (Apache-Spark).

## Using Databricks

Here is an example of building a table in Spark that joins data from `Patient` and `Observation` resources. There is also an [notebook available](fhirnotebook.ipynb) that you can import into Databricks.

First connect to the blob storage where the ndjson files have been exported:

```python
dbutils.fs.mount(
  source = "wasbs://dataexport@<mystorageaccount>.blob.core.windows.net",
  mount_point = "/mnt/dataexport",
  extra_configs = {"fs.azure.account.key.<mystorageaccount>.blob.core.windows.net":"<mystoragekey>"})
```

Create some tables from the ndjson files

```
%sql CREATE TEMPORARY TABLE observationTable USING json OPTIONS (path "/mnt/dataexport/Observation.json")
```

```
%sql CREATE TEMPORARY TABLE patientTable USING json OPTIONS (path "/mnt/dataexport/Patient.json")
```

Select the latest height measurements:

```
%sql 
CREATE OR REPLACE TEMPORARY VIEW temp_heights AS 
  SELECT * FROM (
    SELECT 
      SUBSTRING_INDEX(subject.reference,'/',-1) AS patient, 
      valueQuantity.value as heightValue, 
      valueQuantity.unit as heightUnit,  
      ROW_NUMBER() OVER (PARTITION BY subject.reference ORDER BY issued DESC) AS rn 
   FROM observationTable WHERE code.coding[0].code = "8302-2") tm 
  WHERE tm.rn = 1
```

And weights measurements

```
%sql 
CREATE OR REPLACE TEMPORARY VIEW temp_weights AS 
  SELECT * FROM (
    SELECT 
      SUBSTRING_INDEX(subject.reference,'/',-1) AS patient, 
      valueQuantity.value as weightValue, 
      valueQuantity.unit as weightUnit,  
      ROW_NUMBER() OVER (PARTITION BY subject.reference ORDER BY issued DESC) AS rn 
   FROM observationTable WHERE code.coding[0].code = "29463-7") tm 
  WHERE tm.rn = 1
```

Get the latitude and longitude of each patient and store in temp tables:

```
%sql 
CREATE OR REPLACE TEMPORARY VIEW temp_latitude AS 
  SELECT id, coord.valueDecimal AS latitude FROM 
    (SELECT id, explode(address[0].extension[0].extension) as coord FROM patientTable) 
  WHERE coord.url = 'latitude';
  
CREATE OR REPLACE TEMPORARY VIEW temp_longitude AS 
  SELECT id, coord.valueDecimal AS longitude FROM 
    (SELECT id, explode(address[0].extension[0].extension) as coord FROM patientTable) 
  WHERE coord.url = 'longitude'
```

And finally join all the data

```
%sql 
SELECT 
  patientTable.id, 
  patientTable.name[0].family AS lastName, 
  temp_longitude.longitude AS longitude, 
  temp_latitude.latitude AS latitude, 
  temp_weights.weightValue, temp_heights.heightValue
FROM patientTable 
  INNER JOIN temp_weights ON temp_weights.patient = patientTable.id 
  INNER JOIN temp_heights ON temp_heights.patient = patientTable.id 
  INNER JOIN temp_latitude ON temp_latitude.id = patientTable.id 
  INNER JOIN temp_longitude ON temp_longitude.id = patientTable.id
```
