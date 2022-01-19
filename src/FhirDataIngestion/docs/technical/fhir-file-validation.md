# FHIR file validation using Profiles

`FhirIngestion.Tools` project is converting parquet files to JSON files in FHIR format. Three main stages help to have a successful data convertion: `Convert`, `Anonymize` and `Publish`. `Convert` and `Anonymize` steps create JSON files in FHIR format and modify the content. `Publish` step is used to publish the FHIR format data to the FHIR server.

Anonymizer has a built-in validation step which validates the data after anonymization.

We use profiles to validate FHIR data structure, also files can be used to validate FHIR file format using Profiles.

In order to enable validation for pushing data to FHIR Server, `x-ms-profile-validation: true` header should be added into REST call. This will work on your local FHIR Server as well as FHIR Server hosted on Azure. As of now Bundle Profiles are not validated on server side so we need to use the below to validate Bundle content.

In this document we will show how to use profiles to validate FHIR data structure on Client Side.

To validate FHIR data structure on Client Side, using
[Firely .NET SDK](https://github.com/FirelyTeam/firely-net-sdk) is a good option. It has already built-in features to validate FHIR data structure and profiles. We'll use two different profiles one for Patient another one for Bundle resource type.

We have two sample profile files under [/src/FhirIngestion.Tools.Publisher/Profiles](../../src/FhirIngestion.Tools.Publisher/Profiles).

## PatientProfile.json

```json
{
    "resourceType": "StructureDefinition",
    "url": "https://fhir-data-converter.azurehealthcareapis.com/StructureDefinition/FHIRPatientProfile",
    "name": "FHIRPatientProfile",
    "status": "draft",
    "fhirVersion": "4.0.1",
    "mapping": [
        {
            "identity": "rim",
            "uri": "http://hl7.org/v3",
            "name": "RIM Mapping"
        },
        {
            "identity": "cda",
            "uri": "http://hl7.org/v3/cda",
            "name": "CDA (R2)"
        },
        {
            "identity": "w5",
            "uri": "http://hl7.org/fhir/fivews",
            "name": "FiveWs Pattern Mapping"
        },
        {
            "identity": "v2",
            "uri": "http://hl7.org/v2",
            "name": "HL7 v2 Mapping"
        },
        {
            "identity": "loinc",
            "uri": "http://loinc.org",
            "name": "LOINC code for the element"
        }
    ],
    "kind": "resource",
    "abstract": false,
    "type": "Patient",
    "baseDefinition": "http://hl7.org/fhir/StructureDefinition/Patient",
    "derivation": "constraint",
    "differential": {
        "element": [
            {
                "id": "Patient.identifier",
                "path": "Patient.identifier",
                "min": 1
            },
            {
                "id": "Patient.name",
                "path": "Patient.name",
                "min": 1,
                "max": "1"
            },
            {
                "id": "Patient.gender",
                "path": "Patient.gender",
                "min": 1
            }
        ]
    }
}
```

## BundleProfile.json

```json
{
    "resourceType": "StructureDefinition",
    "url": "https://fhir-data-converter.azurehealthcareapis.com/StructureDefinition/FHIRBundle",
    "name": "FHIRBundle",
    "status": "draft",
    "fhirVersion": "4.0.1",
    "mapping": [
        {
            "identity": "rim",
            "uri": "http://hl7.org/v3",
            "name": "RIM Mapping"
        },
        {
            "identity": "cda",
            "uri": "http://hl7.org/v3/cda",
            "name": "CDA (R2)"
        },
        {
            "identity": "w5",
            "uri": "http://hl7.org/fhir/fivews",
            "name": "FiveWs Pattern Mapping"
        },
        {
            "identity": "v2",
            "uri": "http://hl7.org/v2",
            "name": "HL7 v2 Mapping"
        },
        {
            "identity": "loinc",
            "uri": "http://loinc.org",
            "name": "LOINC code for the element"
        }
    ],
    "kind": "resource",
    "abstract": false,
    "type": "Bundle",
    "baseDefinition": "http://hl7.org/fhir/StructureDefinition/Bundle",
    "derivation": "constraint",
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
                            "https://fhir-data-converter.azurehealthcareapis.com/StructureDefinition/FHIRPatientProfile"
                        ]
                    }
                ]
            }
        ]
    }
}
```

To validate Profiles on Client Side, we need to use the below code. In this sample it's working with a local FHIR Server.

```csharp
FhirJsonParser fp = new FhirJsonParser();

// parse valid Patient payload using FHIRJsonParser
string strPatient = "<< JSON payload for Patient >>";
Patient pat = fp.Parse<Patient>(strPatient);

// parse valid bundle payload using FHIRJsonParser
string strBundle = "<< JSON payload for Bundle >>";
Bundle bund = fp.Parse<Bundle>(strBundle);

// create profile source using `Profiles` folder
var profileSource = new CachedResolver(new MultiResolver(
    new DirectorySource(@"Profiles",
        new DirectorySourceSettings() { IncludeSubDirectories = true }), ZipSource.CreateValidationSource()));

// validation settings
var validationSettings = new ValidationSettings
{
    ResourceResolver = profileSource,
    GenerateSnapshot = true,
    Trace = true,
    ResolveExternalReferences = false
};

var validator = new Hl7.Fhir.Validation.Validator
    (validationSettings);


// validate patient
var result = validator.Validate(pat);
Console.WriteLine(result);

// validate bundle
var resultBundle = validator.Validate(bund);
Console.WriteLine(resultBundle);
```

## Conclusion

There are two ways to validate FHIR resources using Profiles.

1. Server side resource validation using profiles, however as of now FHIR Server doesn't validate Bundle profiles, A bug is created for [Validation error in Bundles using profiles](https://github.com/microsoft/fhir-server/issues/2312).

2. Client side resource validation using profiles, FHIR resources can be validated using [Firely .NET SDK](https://github.com/FirelyTeam/firely-net-sdk).

In `FhirIngestion.Tools.Publisher` implementation, we have used the second option with following code to validate the resources. If bundle is valid we send it to the FHIR Server, if not we're logging the errors.

```csharp
foreach (var file in files)
{
    using var buffer = new StreamReader(file);
    while ((line = buffer.ReadLine()) != null)
    {
        // check if bundle is valid
        bool isValid = ValidateBundle(line, validator, fhirJsonParser);
        if (isValid)
        {
            actionBlock.Post(line);
            validNdJsonLinesCount++;
        }
        else
        {
            MessageHelper.Error("Bundle is not validated by Bundle Profile");
            inValidNdJsonLinesCount++;
        }
    }
}
```
