# Create Liquid Templates for the FHIR Data Ingestion Tool

For the FHIR Data Ingestion Tool we have used [Liquid templates](https://shopify.github.io/liquid/) for data transformation to the FHIR JSON data format. See [the official Shopify reference on the Liquid language](https://shopify.github.io/liquid/basics/introduction/).

## Data

Parsing the Liquid templates combines the template with the provided **data**. The FHIR Data Ingestion Tool can read [Parquet files](https://parquet.apache.org/documentation/latest/) and Comma Separated (CSV) files.

When files are imported into the Parser tool, the name of the file without the extension is taken as the name of the table. The table name is sanitized (only alfabetic characters, no spaces or special characters). If a Parquet file has the same name as an CSV file, it will result in an error that the table already exists.

### Parquet files

Each Parquet files equals 1 data table with one or more fields. Fields can have data types like string, int, date, etecetera.  

### CSV files

Each CSV files equals 1 data table with one or more fields. Each line ends with a newline, and fields are separated by a `,` (comma). The first line of the CSV file must contain the field names, also separated by comma's. All lines must have the same amount of elements.

### Data Model

Once the files are imported, it's made available to Liquid templates by the `Model` object. That contains the `Tables` collection, where every `Table` in the collection has a `FieldNames` collection and a `Records` collection. Each `Record` in the `Records` collection has a `Fields` collection, where every `Field` in that collection has a `Name` and a `Value`.

The Liquid code below iterates through the loaded data model:

```yaml
{% for table in model.Tables -%}
Table '{{ table.Name }}' with fields: {% for field in table.FieldNames -%}{{- field -}}, {% endfor %}
{% for record in table.Records -%}
{% for field in record.Fields -%}{{ field.Value -}}, {%- endfor %}
{% endfor %}
{% endfor -%}
```

The (abbreviated) output of this looks like this:

```yaml
Table 'Claims' with fields: id, identifier, status, type, use, patientId, created, entererId, insurerId, providerId, priority, insurance, total, 
1000,1000.1000.1000,open,4012,,1008,13-05-2021,5000,2020,2030,high,,1234,
2000,2000.2000.2000,closed,4075,,1007,28-11-2020,5001,2021,2031,medium,,5678,
```

## Template(s)

To create a Liquid Template for transforming the data, you need to determine what the starting entity is. It can be a patient, but maybe more logical is to start with the claims. In the code block below we have a simple start where we loop over the claims and show the ID of each claim:

```yaml
{%- for claim in model.claims.Records -%}
  {{ claim.id }}
{% endfor -%}
```

This outputs:

```yaml
1000
2000
3000
4000
5000
6000
7000
8000
9000
```

Once we have the claim, we need to get all the other data linked to the claim. This is for instance:

* Patient
* Insurer
* Provider
* Practitioner

There is also extended information like:

* Healtcare Service
* Organization Affiliation
* Practioner Role

So, for every claim we want to retrieve the linked information for that claim. This results in the next step of our loop over the claims:

```yaml
{%- for claim in model.claims.Records -%}

  {%- assign patient = model.patients.Records | where: "id", claim.patientId | first -%}
  {%- assign enterer = model.practitioners.Records | where: "id", claim.entererId | first -%}
  {%- assign entererRole = model.practitionerRoles.Records | where: "PractitionerId", enterer.id | first -%}
  {%- assign insurer = model.organizations.Records | where: "id", claim.insurerId | first -%}
  {%- assign provider = model.organizations.Records | where: "id", claim.providerId | first -%}

  {%- comment -%}Temporary: show collected information per claim{%- endcomment -%}
  Claim id: {{ claim.identifier }}
  Patient: {{ patient.name }}
  Enterer: {{ enterer.name }}, speciality {{ entererRole.specialty }}
  Insurer: {{ insurer.name }}
  Provider: {{ provider.name }}
{% endfor -%}
```

The (abbreviated) output:

```yaml
Claim id: 1000.1000.1000
  Patient: 
  Enterer: Louis Pasteur, speciality Microbiology
  Insurer: The Insurance Company
  Provider: UK Medical Providers
Claim id: 2000.2000.2000
```

Now we have the data retrieval complete, we want to generate the JSON. We could put that all in one file, but for maintainability it's better to split things up. So we came up with this structure:

* bundle.liquid - this is the main loop over the claims and calls into the components
  * /components
    * claim.liquid
    * patient.liquid
    * ...

Now, let's start working on the `patient.liquid` component. This component would need at least the patient as obtained in the loop we created. This can be passed to the component as a parameter. Something like this:

```yaml
# first clear all definitions from the list
CLEAR

# Select the first claim
ADD {%- assign claim = model.claims.records.first -%}

# get linked information for the claim
ADD {%- assign patient = model.patients.Records | where: "id", claim.patientId | first -%}
ADD {%- assign enterer = model.practitioners.Records | where: "id", claim.entererId | first -%}
ADD {%- assign entererRole = model.practitionerRoles.Records | where: "PractitionerId", enterer.id | first -%}
ADD {%- assign insurer = model.organizations.Records | where: "id", claim.insurerId | first -%}
ADD {%- assign provider = model.organizations.Records | where: "id", claim.providerId | first -%}
```

That doesn't output anything, but if you want to verify what's in the list, use the `LIST` command at the end of the code block.

As you've seen before, this will be prepended for every Liquid code block now. So we can start on a code block just focussing on the patient. This could be like this:

```json
{
    "resource": {
        "resourceType": "Patient",
        "id": "{{ patient.id }}",
        "identifier":{
            "system": "https://fhir-data-converter.azurehealthcareapis.com/patient",
            "value": "{{ patient.id }}"
        },
        "name": [
            {
                "use": "official",
                "given": [ "{{ patient.firstName }}" ],
                "family": "{{ patient.lastName }}"
            }
        ],
        "telecom": [
            {
            "system": "phone",
            "value": "{{ patient.phone }}"
            }
        ],
        "gender": "{{ patient.gender }}",
        "birthDate": "{{ patient.birthDate }}",
        "address": [
            {
                {% if patient.city != blank %}"city": "{{ patient.city }}",{% endif %}
                {% if patient.zipcode != blank %}"postalCode":"{{ patient.zipcode }}",{% endif %}
                "country": "United Kingdom"
            }
        ]
    },
    "request": {
      "method": "PUT",
      "url": "Patient/{{ patient.id }}"
    }
}
```

This results in this output:

```json
{
  "resource": {
    "resourceType": "Patient",
    "id": "1008",
    "identifier": {
      "system": "https://fhir-data-converter.azurehealthcareapis.com/patient",
      "value": "1008"
    },
    "name": [
      {
        "use": "official",
        "given": [
          "Paul"
        ],
        "family": "Clark"
      }
    ],
    "telecom": [
      {
        "system": "phone",
        "value": "7454 662873"
      }
    ],
    "gender": "M",
    "birthDate": "29-11-2013",
    "address": [
      {
        "city": "Orford",
        "postalCode": "WA2 9BS",
        "country": "United Kingdom"
      }
    ]
  },
  "request": {
    "method": "PUT",
    "url": "Patient/1008"
  }
}
```

Although this first version looks nice, we have an issue here. The gender-field shows "M" (or "F"), but the FHIR data requires "male" or "female". So we need to convert the data. For this we add **data converters** to the structure of Liquid files. Again a component we just use for this purpose. This is what the data converter for gender could look like. When you render this, what do you expect the output to be? Try it, and figure out why that is selected.

```yaml
{%- case gender -%}
    {%- when "M" -%}male
    {%- when "F" -%}female
    {%- else -%}other
{%- endcase -%}
```

Of course it has to do with the field `gender` not being set. So you end up in the "else" branch. This component is saved in the fake data set in `Templates\data-converters\gender.liquid`. We can reuse this in the Liquid code we created before for the patient. It is changed into the code below. Focus on the 'gender' field.

```yaml
{
    "resource": {
        "resourceType": "Patient",
        "id": "{{ patient.id }}",
        "identifier":{
            "system": "https://fhir-data-converter.azurehealthcareapis.com/patient",
            "value": "{{ patient.id }}"
        },
        "name": [
            {
                "use": "official",
                "given": [ "{{ patient.firstName }}" ],
                "family": "{{ patient.lastName }}"
            }
        ],
        "telecom": [
            {
            "system": "phone",
            "value": "{{ patient.phone }}"
            }
        ],
        {%- comment -%}Here we added the data converter for the gender field.{%- endcomment -%}
        "gender": "{% include 'data-converters/gender', gender: patient.gender -%}",
        "birthDate": "{{ patient.birthDate }}",
        "address": [
            {
                {% if patient.city != blank %}"city": "{{ patient.city }}",{% endif %}
                {% if patient.zipcode != blank %}"postalCode":"{{ patient.zipcode }}",{% endif %}
                "country": "United Kingdom"
            }
        ]
    },
    "request": {
      "method": "PUT",
      "url": "Patient/{{ patient.id }}"
    }
}
```

The output is now:

```json
{
  "resource": {
    "resourceType": "Patient",
    "id": "1008",
    "identifier": {
      "system": "https://fhir-data-converter.azurehealthcareapis.com/patient",
      "value": "1008"
    },
    "name": [
      {
        "use": "official",
        "given": [
          "Paul"
        ],
        "family": "Clark"
      }
    ],
    "telecom": [
      {
        "system": "phone",
        "value": "7454 662873"
      }
    ],
    "gender": "male",
    "birthDate": "29-11-2013",
    "address": [
      {
        "city": "Orford",
        "postalCode": "WA2 9BS",
        "country": "United Kingdom"
      }
    ]
  },
  "request": {
    "method": "PUT",
    "url": "Patient/1008"
  }
}
```

You see that the gender is now properly formatted for the FHIR format.

Now that we know about components and data-converters, let's go back to the main bundle. We looped over the claims and retrieved the linked data for a claim. Now we can add the patient component to it. The Ingestion Tool uses the bundle structure to send a claim and all linked data. So for each claim we create a bundle and start adding the patient data. Run the code below, but ... something will happen that you might not expect (read on after the code :)

```yaml
{%- for claim in model.claims.Records -%}

  {%- assign patient = model.patients.Records | where: "id", claim.patientId | first -%}
  {%- assign enterer = model.practitioners.Records | where: "id", claim.entererId | first -%}
  {%- assign entererRole = model.practitionerRoles.Records | where: "PractitionerId", enterer.id | first -%}
  {%- assign insurer = model.organizations.Records | where: "id", claim.insurerId | first -%}
  {%- assign provider = model.organizations.Records | where: "id", claim.providerId | first -%}

  {
    "resourceType": "Bundle",
    "type": "batch",
    "entry": [
        {% include 'components/patient', patient: patient %}
    ]
  }
{% endfor -%}
```

The (abbreviated) output shows:

```json
Process output error: SyntaxError: Unexpected token { in JSON at position 910. Fallback to plain text

{
    "resourceType": "Bundle",
    "type": "batch",
    "entry": [
```

What? An error? What did we do wrong? Well, this is about the JSON structure. We generate something like "{ ... }" followed by "{ ... }" for the next patient. This is not proper JSON. But we actually want to generate NDJSON, which is a file format containing multiple JSON structures separated by newlines. That also means that we should collapse the JSON of 1 claim to 1 line.

This is solved with a `capture` statement and a few filters. We want to capture the JSON output, and then flatten it. We do that like this:

``` yaml
{%- comment -%}This is the for-loop over all the claims{%- endcomment -%}
{%- for claim in model.claims.Records -%}

  {%- comment -%}Here we get all the linked information for the claim{%- endcomment -%}
  {%- assign patient = model.patients.Records | where: "id", claim.patientId | first -%}
  {%- assign enterer = model.practitioners.Records | where: "id", claim.entererId | first -%}
  {%- assign entererRole = model.practitionerRoles.Records | where: "PractitionerId", enterer.id | first -%}
  {%- assign insurer = model.organizations.Records | where: "id", claim.insurerId | first -%}
  {%- assign provider = model.organizations.Records | where: "id", claim.providerId | first -%}
  
  {%- comment -%}This is to capture the output of the FHIR Claim JSON{%- endcomment -%}
  {% capture claimOutput -%}
  {
    "resourceType": "Bundle",
    "type": "batch",
    "entry": [
      {% include 'components/patient', id: patient.id, patient: patient %}
    ]
  }
  {% endcapture -%}
  {%- comment -%}With this command we output the captured JSON, 
  remove all double spaces and strip newlines. This results in one line of JSON per claim.{%- endcomment -%}
  {{ claimOutput | remove: "  " | strip_newlines }}
{% endfor -%}  
```

The (abbreviated) output is now:

```json
Process output error: SyntaxError: Unexpected token { in JSON at position 460. Fallback to plain text

{"resourceType": "Bundle","type": "batch","entry": [{"resource": {"resourceType": "Patient","id": "1008","identifier":{"system": "/patient","value": "-"},"name": [{"use": "official","given": [ "Paul" ],"family": "Clark"}],"telecom": [{"system": "phone","value": "7454 662873"}],"gender": "male","birthDate": "29-11-2013","address": [{"city": "Orford","postalCode":"WA2 9BS","country": "United Kingdom"}]},"request": {"method": "PUT","url": "Patient/1008"}}]}
{"resourceType": "Bundle","type": "batch","entry": [{"resource": {"resourceType": "Patient","id": "1007","identifier":{"system": "/patient","value": "-"},"name": [{"use": "official","given": [ "Ava" ],"family": "Brown"}],"telecom": [{"system": "phone","value": "7213 072284"}],"gender": "female","birthDate": "28-11-2012","address": [{"city": "St Helens","postalCode":"WA11 7EY","country": "United Kingdom"}]},"request": {"method": "PUT","url": "Patient/1007"}}]}
```

Of course we still get a JSON error, as this is not JSON but NDJSON. You can copy 1 line of JSON (hint - or not loop claims and do just one) and copy that into a JSON file. Then you can inspect that.

Now let's add a claims component. We won't go through it in detail like patient. You probably get the idea now. The code of the claim (simplified) is below.

```json
{
      "resource": {
        "resourceType": "Claim",
        "id": "{{ claim.id }}",
        "identifier": [
          { 
            "system": "https://fhir-data-converter.azurehealthcareapis.com/claim", 
            "value": "{{ claim.id }}" }
        ],
        "status":  "{{ claim.status }}",
        "type": {
          "coding": [
            {
              "system": "http://terminology.hl7.org/CodeSystem/claim-type",
              "code": "oral"
            }
          ]
        },
        "use": "claim",
        "created": "{{ "now" | date: "%Y-%m-%d %H:%M:%S" }}",
        "patient": { "reference": "Patient/{{ patient.id }}" },
        "priority": { "coding": [{ "code": "normal" }] }
      },
      "request": { "method": "PUT", "url": "Claim/{{ claim.id }}" }
}
```

This will output this JSON:

```json
{
  "resource": {
    "resourceType": "Claim",
    "id": "1000",
    "identifier": [
      {
        "system": "https://fhir-data-converter.azurehealthcareapis.com/claim",
        "value": "1000"
      }
    ],
    "status": "open",
    "type": {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/claim-type",
          "code": "oral"
        }
      ]
    },
    "use": "claim",
    "created": "2021-12-08 13:59:28",
    "patient": {
      "reference": "Patient/1008"
    },
    "priority": {
      "coding": [
        {
          "code": "normal"
        }
      ]
    }
  },
  "request": {
    "method": "PUT",
    "url": "Claim/1000"
  }
}
```

So let's include the claim.liquid file. Copy one item again and inspect the JSON. Now it's still wrong. Can you see why?

```yaml
{%- comment -%}This is the for-loop over all the claims{%- endcomment -%}
{%- for claim in model.claims.Records -%}

  {%- comment -%}Here we get all the linked information for the claim{%- endcomment -%}
  {%- assign patient = model.patients.Records | where: "id", claim.patientId | first -%}
  {%- assign enterer = model.practitioners.Records | where: "id", claim.entererId | first -%}
  {%- assign entererRole = model.practitionerRoles.Records | where: "PractitionerId", enterer.id | first -%}
  {%- assign insurer = model.organizations.Records | where: "id", claim.insurerId | first -%}
  {%- assign provider = model.organizations.Records | where: "id", claim.providerId | first -%}
  
  {%- comment -%}This is to capture the output of the FHIR Claim JSON{%- endcomment -%}
  {% capture claimOutput -%}
  {
    "resourceType": "Bundle",
    "type": "batch",
    "entry": [
      {% include 'components/patient', id: patient.id, patient: patient %}
      {% include 'components/claim', id: claim.id, claim: claim %}
    ]
  }
  {% endcapture -%}
  {%- comment -%}With this command we output the captured JSON, 
  remove all double spaces and strip newlines. This results in one line of JSON per claim.{%- endcomment -%}
  {{ claimOutput | remove: "  " | strip_newlines }}
{% endfor -%}  
```

This will output this (abbreviated):

```json
Process output error: SyntaxError: Unexpected token { in JSON at position 456. Fallback to plain text

{"resourceType": "Bundle","type": "batch","entry": [{"resource": {"resourceType": "Patient","id": "1008","identifier":{"system": "/patient","value": "-"},"name": [{"use": "official","given": [ "Paul" ],"family": "Clark"}],"telecom": [{"system": "phone","value": "7454 662873"}],"gender": "male","birthDate": "29-11-2013","address": [{"city": "Orford","postalCode":"WA2 9BS","country": "United Kingdom"}]},"request": {"method": "PUT","url": "Patient/1008"}}{"resource": {"resourceType": "Claim","id": "1000","identifier": [{ "system": "https://fhir-data-converter.azurehealthcareapis.com/claim", "value": "1000" }],"status":"open","type": {"coding": [{"system": "http://terminology.hl7.org/CodeSystem/claim-type","code": "oral"}]},"use": "claim","created": "2021-12-08 14:00:51","patient": { "reference": "Patient/1008" },"priority": { "coding": [{ "code": "normal" }] }},"request": { "method": "PUT", "url": "Claim/1000" }}]}
```

The patient and claim are both entries in a collection. So they should be separate by a comma delimiter. Sometimes we might have option entries or multiple of the same type. So for this purpose we have a construct with a delimiter variable. The idea is to prepend a delimiter when it's a consequetive item. The variable will hold the delimiter to prepend. And we start with an empty delimiter. This is the new bundle:

```yaml
{%- comment -%}This is the for-loop over all the claims{%- endcomment -%}
{%- for claim in model.claims.Records -%}

  {%- comment -%}Define the delimiter as blank{%- endcomment -%}
  {% assign delimiter = "" -%}

  {%- comment -%}Here we get all the linked information for the claim{%- endcomment -%}
  {%- assign patient = model.patients.Records | where: "id", claim.patientId | first -%}
  {%- assign enterer = model.practitioners.Records | where: "id", claim.entererId | first -%}
  {%- assign entererRole = model.practitionerRoles.Records | where: "PractitionerId", enterer.id | first -%}
  {%- assign insurer = model.organizations.Records | where: "id", claim.insurerId | first -%}
  {%- assign provider = model.organizations.Records | where: "id", claim.providerId | first -%}
  
  {%- comment -%}This is to capture the output of the FHIR Claim JSON{%- endcomment -%}
  {% capture claimOutput -%}
  {
    "resourceType": "Bundle",
    "type": "batch",
    "entry": [
        {%- comment -%}This line will output the current delimiter, and always set it to , for the next{%- endcomment -%}
        {{ delimiter }}{% assign delimiter = "," %}
        {% include 'components/patient', id: patient.id, patient: patient %}

        {%- comment -%}And we do that before every component we output{%- endcomment -%}
        {{ delimiter }}{% assign delimiter = "," %}
        {% include 'components/claim', id: claim.id, claim: claim %}
    ]
  }
  {% endcapture -%}
  {%- comment -%}With this command we output the captured JSON, 
  remove all double spaces and strip newlines. This results in one line of JSON per claim.{%- endcomment -%}
  {{ claimOutput | remove: "  " | strip_newlines }}
{% endfor -%}
```

This will output. This will look like 'an error', but we know now this is valid NDJSON

```json
Process output error: SyntaxError: Unexpected token { in JSON at position 915. Fallback to plain text

{"resourceType": "Bundle","type": "batch","entry": [{"resource": {"resourceType": "Patient","id": "1008","identifier":{"system": "/patient","value": "-"},"name": [{"use": "official","given": [ "Paul" ],"family": "Clark"}],"telecom": [{"system": "phone","value": "7454 662873"}],"gender": "male","birthDate": "29-11-2013","address": [{"city": "Orford","postalCode":"WA2 9BS","country": "United Kingdom"}]},"request": {"method": "PUT","url": "Patient/1008"}},{"resource": {"resourceType": "Claim","id": "1000","identifier": [{ "system": "https://fhir-data-converter.azurehealthcareapis.com/claim", "value": "1000" }],"status":"open","type": {"coding": [{"system": "http://terminology.hl7.org/CodeSystem/claim-type","code": "oral"}]},"use": "claim","created": "2021-12-08 14:02:08","patient": { "reference": "Patient/1008" },"priority": { "coding": [{ "code": "normal" }] }},"request": { "method": "PUT", "url": "Claim/1000" }}]}
```

Now you have learned the basics of the setup of creating template(s) for the FHIR Data Ingestion Tool. For further reference, have a look at the templates we have created in the sample. And ... enjoy Liquid :)
