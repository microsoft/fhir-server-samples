# Creating Bundle files

## Overview

`FhirIngestion.Tools` project is converting Parquet / CSV files to FHIR Bundle resource type. In this document we'll describe how to create bundles and validate them. First make sure you have proper setup for your application and follow the instructions in [Setup the Claims Data Ingestion Tool](../getting-started/README.md) for details on how to install this tool. The [Configuration file section](../getting-started/README.md#Configuration_file) has 3 stages which are `converter`, `anonymizer` and `publisher`.

### 1. Converter stage

In this stage parquet files in `Input` folder grabbed and converted to FHIR Bundle resource type using liquid templates. `Input` folder is defined in the [Configuration file section](../getting-started/README.md#Configuration_file).

Liquid templates are located in the [liquid-templates](../../config/templates) folder. The `Converted` folder under `Output` is used to store converted bundles, location is also defined in the [Configuration file section](../getting-started/README.md#Configuration_file). All the converted bundles are stored as single lines in `.ndjson` format.

### 2. Anonymizer stage

Anonymizer stage uses `converted` `.ndjson` data from the `converter` stage as input and anonymizes the selected fields as described in the [anonymizer documentation](anonymizer-config-file.md).

The file ["/config/anonymizer/anonymizer.json"](../../config/anonymizer/anonymizer.json) is used to define which fields should be anonymized.

All anonymized files are located in the `anonymized` folder under the `Output` folder. All anonymized files are stored as single lines in `.ndjson` format.

### 3. Publisher stage

Publisher stage uses `.ndjson` data from the `anonymized` folder as input and read the whole file. It then publishes the data to the FHIR server with multiple threads.

Before publishing the data to the FHIR server we validate all resources using Profiles. Profiles are defined in the [FHIR file validation and are using Profiles document](fhir-file-validation.md).

## Defining Profiles for Bundles

FHIR format has a feature called Profiles where you can define your rules for any resource type. Profiles are used to define your ruleset and validate the data based on these rules. When you want to define required fields and validate with different rulesets, `Profiles` is a good option.

> **IMPORTANT:** Based on [Supported FHIR Features](https://docs.microsoft.com/en-us/azure/healthcare-apis/fhir/fhir-features-supported), each bundle is limited to 500 items.

As an example we're using Patient Profile, individually you can test and validate your content using this profile in meta like below:

```json
...
"meta":{
    "profile":["https://example.org/fhir/StructureDefinition/FHIRPatientProfile"]
}
...
```

You can validate patient data using this profile on server side using `x-ms-profile-validation: true` header in the request. Also you can validate patient data using this profile on client side using [Firely .NET SDK](https://github.com/FirelyTeam/firely-net-sdk/).

If you would like to validate your Patient Profile in a Bundle, you need to refer your Patient profile in the Bundle Profile.

```json
...
 "differential": {
    "element": [
      {
        "id": "Bundle.entry:patient",
        "path": "Bundle.entry.patient",
        "sliceName": "patient",
        "min": 0,
        "max": "1"
      },
      {
        "id": "Bundle.entry:patient.resource",
        "path": "Bundle.entry.patient.resource",
        "min": 0,
        "max": "1",
        "type": [
          {
            "code": "Patient",
            "profile": [
              "https://fhir-data-converter.azurehealthcareapis.com/FHIRPatientProfile"
            ]
          }
        ]
      }
    ]
  }
...
```

We have sample profile files defined under [/src/FhirIngestion.Tools.Publisher/Profiles](../../src/FhirIngestion.Tools.Publisher/Profiles)

> **IMPORTANT:** These profiles are defined in liquid templates manually. If you would like to introduce your new profile, please add those into the `Profile` folder and add into the liquid templates.

## Defining Extensions

FHIR format has a feature called extensions to define additional fields other than common fields provided in FHIR format. If you would like to define additional fields for reference in resource types such as template version, template creation date, target market, etc. you need to define those extensions fields in resource types.

These extensions are defined in Liquid template files, [/config/templates/bundle.liquid](../../config/templates/bundle.liquid) file has variables for these extension values:

```json
{%- comment -%} metadata for fhir templates {%- endcomment -%}
{% assign templateDate = "2021-12-01" | date: "%Y-%m-%d" -%}
{% assign templateVersion = "1.0.0" -%}
{% assign templateTargetMarket = "Spain" -%}
```

After we assign these values to variables, we can define these extensions in resource files:

```json
"extension": [
    {
        "url": "{{identiferSystemUri}}/StructureDefinition/templateVersion",
        "valueString": "{{ templateVersion }}"
    },
    {
        "url": "{{identiferSystemUri}}/StructureDefinition/templateDate",
        "valueString": "{{ templateDate }}"
    },
    {
        "url": "{{identiferSystemUri}}/StructureDefinition/templateTargetMarket",
        "valueString": "{{ templateTargetMarket }}"
    }
],
...
```

These extensions are added to all individual resources in the bundle, you can decide for each resource type to keep these extensions or not.

> **IMPORTANT:** These extensions are defined in liquid templates manually. If you would like to introduce your new extension, please add those into `bundle.liquid` file and bind the values in different resource types under the `components` folder.

## Posting Bundles files to FHIR server

We're using `Azure API for FHIR` as a server. And Azure API for FHIR is a REST API. So we're using `POST` method to post the bundle file to FHIR server. This API is currently not supporting `transaction` method. Instead of `transaction` we use `batch` method. The difference between both is on how the data will be processed:

* In the case of `transaction`, if one element fails, all the bundle fails
* In the case of `batch`, it will upload anything valid and won't upload the failed one

This may create `Patient`, `Practitioner` and `Organization` without creating the associated `Claims` for example. In all the cases, the data will continue to be consistant as they are created from no dependence to dependent ones.

If the tool is run a second time with corrected data the `batch` will update any existing one.

Please find recent [Supported FHIR Features](https://docs.microsoft.com/en-us/azure/healthcare-apis/fhir/fhir-features-supported) in the documentation.

The following lines will give you elements to test those 2 methods and adjust them if you want, once the `transaction` will be supported if that's the preferred method.

To post Bundle files with `batch` to FHIR Server, we need to use `POST` method to root:

> POST <https://fhir-data-converter.azurehealthcareapis.com>

If we would like to post Bundle files with `transaction` to FHIR Server, we need to use `POST` method:

> POST <https://fhir-data-converter.azurehealthcareapis.com/Bundle>

As of now we create both bundle files with `batch` and `transaction` methods. Only difference is that `batch` method is used to post multiple resources in one request and create multiple resources at the same time, however `transaction` only creates bundle file and resource types in that bundle are not available individually.

Please find more information at [HL7:FHIR Bundle type reference](http://hl7.org/fhir/valueset-bundle-type.html)

> **IMPORTANT:** Once the Azure FHIR server will support `transaction`, you will be able to decide your bundle strategy:
>
> * `transaction` fails whole request if there's one element that fail to be created
> * `batch` fails individual element and will create all those not failing

## Test individual resource types

Bundle includes individual resource types, all individual resource types have unique `id` fields, so we use these indivudual resource ids to for PUT requests to FHIR server. If it's exists, it'll update the existing one and `versionId` will be incremented. If it's not exists, it'll create a new one.

A sample bundle template looks like this:

```json
{
  "resourceType": "Bundle",
  "type": "batch",
  "entry": [
    {
        "resource": {
            "resourceType": "Patient",
            "id": "1002",

           ...

        },
        "request": {
            "method": "PUT",
            "url": "Patient/1002"
        }
    },
    
    ....
    
    ,{
        "resource": {
            "resourceType": "Claim",
            "id": "1112",

            ...

            },
        "request": {
            "method": "PUT",
            "url": "Claim/1112"
        }
    }
  ],
  "meta": {
    "security": [
        {
            "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            "code": "REDACTED",
            "display": "redacted"
        },
        {
            "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
            "code": "CRYTOHASH",
            "display": "cryptographic hash function"
        },
        {
            "code": "PERTURBED",
            "display": "exact value is replaced with another exact value"
        },
        {
            "code": "GENERALIZED",
            "display": "exact value is replaced with a general value"
        }
    ]
  }
}
```

Using `batch` will bring all the resources in the bundle together and create them at the same time. We can track individual resource creation by using `entry.response.status` field.

```json
  "response": {
      "status": "200",
      "etag": "W/\"88\"",
      "lastModified": "2021-12-07T10:45:39+00:00"
  }
```

### 1. Test "Claim" resource

We have a `Claim` resource in the bundle. For `Claim` resource, we use `PUT` method to update/create the resource in the bundle, structure looks like this:

```json
{
"resource": {
    "resourceType": "Claim",
    "id": "<<claimId>>",

...

"request": {
  "method": "PUT",
  "url": "Claim/<<claimId>>"
}
```

After we get successful response from POST request like below

```json
  "response": {
      "status": "200",
      "etag": "W/\"88\"",
      "lastModified": "2021-12-07T10:45:39+00:00"
  }
```

We can use `GET` method to get the resource back.

> GET <https://fhir-data-converter.azurehealthcareapis.com/Claim/{{claimId}}>

```json
{
  "resourceType": "Claim",
  "id": "<<claimId>>",
  "meta": {
      "versionId": "4",
      "lastUpdated": "2021-12-07T11:00:08.015+00:00",
  
  ...
}
```

`meta.versionId` is incremented by 1 each time we update the resource. We can keep track of history using version Id. Using `versionId` we can get the previous specific version of the resource.

> GET <https://fhir-data-converter.azurehealthcareapis.com/Claim/{{claimId}}/_history/{{versionId}}>

Response:

```json
{
    "resourceType": "Claim",
    "id": "<<claimId>>",
    "meta": {
        "versionId": "<<versionId>>",
        "lastUpdated": "2021-12-07T10:45:39.384+00:00",
        ...
```

> NOTE: `meta.versionID` is available for all resource types. You can retrieve previous versions of the resource by using `{FHIR_SERVER}/{ResourceType}/_history/{versionId}` endpoint.

### 2. Test "Patient" resource

We have a `Patient` resource in the bundle. For `Patient` resource, we use `PUT` method to update/create the resource in the bundle, structure looks like this:

```json
{
"resource": {
    "resourceType": "Patient",
    "id": "<<patientid>>",

...

"request": {
  "method": "PUT",
  "url": "Patient/<<patientid>>"
}
```

After we get successful response from POST request like below

```json
  "response": {
      "status": "200",
      "etag": "W/\"88\"",
      "lastModified": "2021-12-07T10:45:39+00:00"
  }
```

We can use `GET` method to get the resource back.

> GET <https://fhir-data-converter.azurehealthcareapis.com/Patient/{{patientId}}>

```json
{
  "resourceType": "Patient",
  "id": "<<patientId>>",
  "meta": {
      "versionId": "4",
      "lastUpdated": "2021-12-07T11:00:08.015+00:00",
  ...
}
```

### 3. Test "Organization" resource

We have two `Organization` resources in the bundle. One for insurer and another one for provider. For `Organization` resource, we use `PUT` method to update/create the resource in the bundle, structure looks like this:

```json
{
"resource": {
    "resourceType": "Organization",
    "id": "<<organizationid>>",

...

"request": {
  "method": "PUT",
  "url": "Organization/<<organizationid>>"
}
```

After we get successful response from POST request like below

```json
  "response": {
      "status": "200",
      "etag": "W/\"88\"",
      "lastModified": "2021-12-07T10:45:39+00:00"
  }
```

We can use `GET` method to get the resource back.

> GET <https://fhir-data-converter.azurehealthcareapis.com/Organization/{{organizationid}}>

```json
{
  "resourceType": "Organization",
  "id": "<<organizationid>>",
  "meta": {
      "versionId": "4",
      "lastUpdated": "2021-12-07T11:00:08.015+00:00",
  
  ...
}
```

### 4. Test "Practitioner" resource

We have a `Practitioner` resources in the bundle. For `Practitioner` resource, we use `PUT` method to update/create the resource in the bundle:

Use `GET` method to get the resource back.

> GET <https://fhir-data-converter.azurehealthcareapis.com/Practitioner/{{practitionerId}}>

Response:

```json
{
  "resourceType": "Practitioner",
  "id": "<<practitionerId>>",
  "meta": {
      "versionId": "2",
      "lastUpdated": "2021-12-07T11:00:08.015+00:00",
  ...
}
```

## Conclusion

Using `Bundles` to send multiple resources to Azure Healthcare APIs is a great way to reduce the number of HTTP requests. In our implementation we're reducing the number of HTTP requests by using `Bundles` and making sure all the resources are sent in the same bundle.

Make sure you reflect your all changes into the bundle.liquid template [config/templates/bundle.liquid](../../config/templates/bundle.liquid) or create your own liquid template to reflect your new dataset.
