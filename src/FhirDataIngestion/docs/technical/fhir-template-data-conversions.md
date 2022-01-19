# Data conversions in Liquid templates

Liquid templates are helping to map Parquet and CSV data files to the FHIR json format. Data comes from a source and is mapped directly using Liquid language in templates. FHIR is a standard for health care data exchange, published by HL7®, reference for all resource types can be found [here](http://hl7.org/fhir).

In some scenarios data can be saved in a different formats than FHIR standards. For example gender data in a field is using `M` or `F`, but FHIR format accepts `male` and `female`. In this case, data converters modules convert input data to valid FHIR field values.

Liquid templates have "control flow tags" to create conditions that decide whether blocks of Liquid code get executed. [If/else](https://shopify.github.io/liquid/tags/control-flow/#if) statement and [case/when](https://shopify.github.io/liquid/tags/control-flow/) statement are two control flow tags.

This document explains some options for data conversion in the current Liquid template structure. Converters are extracted in different liquid files, so these seperated files will be helpful for simplicity of the main liquid file. There is a dedicated folder to store data converters, it is called [config/templates](../../config/templates).

Data conversions are used in the following examples.

## 1. Gender Mapping

In the main Liquid template, converter is used by adding `include` to referencing converter and passing the parameter. `data-converters/gender` refers `gender.liquid` file in the `data-converters` folder, and `gender` is passed as variable.

```json
"gender": "{% include 'data-converters/gender', gender: patient.gender -%}",
```

In `data-converters` folder, `gender.liquid` contains a case like below, `"M"` and `"F"` letters are passed to be mapped to `"male"` and `"female"`. If there's an invalid case value will be mapped to `"other"`.

```json
{%- case gender -%}
    {%- when "M" -%}male
    {%- when "F" -%}female
    {%- else -%}other
{%- endcase -%}
```

## 2. Date Mapping

In the main Liquid template, converter is used by adding `include` to reference converter and passing the parameter. `data-converters/convertdate` refers `convertdate.liquid` file in the `data-converters` folder.

In case of date, contains date a different format, this date convert sample can be helpful.

```json
"created": "{% include 'data-converters/convertdate', date: claim.date -%}",
```

`claim.date` is passed as a parameter and in the sample  `"now"` is used as an example. If any date mapping is needed [Liquid template - date format](https://shopify.github.io/liquid/filters/date/) document can be helpful.

```json
{{ "now" | date: "%Y-%m-%d" }}
```

## 3. PayeeType Mapping

Another boolean sample, `"payeeType"` is passed as boolean value which contains `"true"`, `"false"` to be mapped to `"draft"`, `"active"` and `"entered-in-error"` values.

```json
"payee": { "type": { "coding": [{ "code": "{% include 'data-converters/payeetype', payeeType: payeeType -%}" }] } },        
```

Boolean values are mapped to below values.

```json
{%- case claimStatus -%}
    {%- when false -%}draft
    {%- when true -%}active
    {%- else -%}entered-in-error
{%- endcase -%}
```

## 4. If/Elsif/else condition

Other than using `case/when` statement, `if`, `elsif` and `else` tags can be used to execute a block of code only if a certain condition is true.

```json
{% if customer.name == "kevin" %}
  Hey Kevin!
{% elsif customer.name == "anonymous" %}
  Hey Anonymous!
{% else %}
  Hi Stranger!
{% endif %}
```

## Conclusion

For small datasets `if/else` and `case/when` statements are used to control the flow of Liquid. These tags can be used to create conditions that decide whether blocks of Liquid code get executed. For large datasets a different approach can be used via storing values in `Parquet` file or `CSV` file, loading data and map them through look-up using where conditions.
