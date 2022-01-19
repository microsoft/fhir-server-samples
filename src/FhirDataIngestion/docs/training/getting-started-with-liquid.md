# Getting Started with Liquid

[Liquid](https://shopify.github.io/liquid/) is an open-source template language created by [Shopify](https://www.shopify.com/). The input is text based combined with Liquid commands. The text can be anything, like HTML, CSS, JSON and more. Shopify provided a full [reference to the Liquid language](https://shopify.github.io/liquid/basics/introduction/), but in this document we'll go through the basics with some samples.

Liquid templates have (usually) the extension `.liquid` and the language is a combination of **objects**, **tags** and **filters**. In the rest of this document we show some basic use of these components.

## Objects

Objects contain the content that Liquid displays on a page. Objects and variables are displayed when enclosed in double curly braces: {{ and }}. Below an example of an object, which you might recognize as a string. It's a bit of a useless use of Liquid, but it demonstrates the basic use:

```yaml
This is sample output of just a string {{ "Hello Liquid!" }}
```

The output looks like this:

```yaml
This is sample output of just a string Hello Liquid!
```

But using objects makes more sense when using real data. The parsing process normally takes the Liquid template as input, but also one or more named data objects that can then be accessed by the template. Below an example where we access the `Model` object and its `Tables` collection to show the name of the first table:

```yaml
This is the first table name: {{ model.Tables[0].Name }}
```

Using the fake dataset, the output is:

```yaml
This is the first table name: Claims
```

It's important to be aware that Liquid parsers will never output errors references to non-existing data, except for index out of bounds. For instance, below you see an example of the use of a non existing field and it's output:

```yaml
The field NON_EXISTING_FIELD does not exist and outputs '{{ NON_EXISTING_FIELD }}' (so, nothing :D)
```

## Tags

Tags create the logic and control flow for templates. The curly brace percentage delimiters **{%** and **%}** and the text that they surround do not produce any visible output when the template is rendered. This lets you assign variables and create conditions or loops without showing any of the Liquid logic on the page.

### Variable tags

Variable tags create new Liquid variables that can be referenced in the rest of the code.

* assign - Creates a new named variable.
* capture - Captures the string inside of the opening and closing tags and assigns it to a string variable.
* increment/decrement - Creates and outputs a counter with initial value 0. On subsequent calls, it increases/decreases its value by one and outputs the new value.

See below how these can be used and the output.

```yaml
{%- comment -%}Assign{%- endcomment -%}
{% assign name = "Pete Johnson" -%}
Name: {{ name }}

{%- comment -%}Capture{%- endcomment -%}
{% capture full_sentence %}This is some text combined with the variable name with value '{{ name }}'{% endcapture %}
Full sentence: {{ full_sentence }}

{% comment -%}Increment/decrement{%- endcomment -%}
First call: {% increment index %}
Second call: {% increment index %}
Third call: {% increment index %}
Fourth call (Decrement): {% decrement index -%}
```

The output of this:

```yaml
Name: Pete Johnson
Full sentence: This is some text combined with the variable name with value 'Pete Johnson'

First call: 0
Second call: 1
Third call: 2
Fourth call (Decrement): 1
```

### Control flow tags

Control flow tags create conditions that decide whether blocks of Liquid code get executed.

* if / else / elsif - Executes a block of code only if a certain condition is true and add conditions.
* case / when - Creates a switch statement to execute a particular block of code when a variable has a specified value. case initializes the switch statement, and when statements define the various conditions. An optional else statement at the end of the case provides code to execute if none of the conditions are met.

See below how these can be used and the output.

```yaml
{% assign name = "anonymous" -%}
{%- if name == "Pete" %}
  Hey Pete!
{% elsif name == "anonymous" %}
  Why don't you tell me your name?
{% else %}
  I don't know who you are...
{% endif %}
```

### Template tags

Template tags tell Liquid where to disable processing for comments or non-Liquid markup, and how to establish relations among template files.

* comment - adding comment to your template that won't be rendered.
* include - Insert the rendered content of another template file within the current template.

> **NOTE:** Include is marked obsolete and should be replaced by `render`, however currently it's not working yet. Working with the Fluid team on this [issue](https://github.com/sebastienros/fluid/issues/420).

See below how these can be used and the output.

```yaml
{%- comment -%}You've seen comment before, but here it is again{%- endcomment -%}

{%- comment -%}Render{%- endcomment -%}
{%- comment -%}In this case we include an external template to convert the gender to text{%- endcomment -%}
{%- comment -%}The external template is part of the fake dataset.{%- endcomment -%}
{%- assign patient = model.Patients.Records | first -%}
Patient with gender '{{ patient.gender }}' translated to '{% include 'data-converters/gender', gender: patient.gender %}'
```

This results in this output:

```yaml
Patient with gender 'M' translated to 'male'
```

### Iteration tags

Iteration tags repeatedly run blocks of code.

* for - Repeatedly executes a block of code.
* else - Specifies a fallback case for a for loop which will run if the loop has zero length.

See below how these can be used and the output.

```yaml
{%- comment -%}for loop{%- endcomment -%}
Tables in the model:
{% for table in model.Tables -%}
{{ table.Name }}
{% endfor -%}

{%- comment -%}Make a selection from patients that don't exist (empty array) and process.{%- endcomment -%}
{%- comment -%}For more information on the where filter, see the Filters section below.{%- endcomment -%}
{% assign emptyArray = model.Patients | where: "name", "nobody" %}
The result of the loop-else for an empty array:
{% for item in emptyArray -%}
    {{ item.Name }}
{% else %}
    Found nobody with that name.
{% endfor -%}
```

The output:

```yaml
Tables in the model:
Claims
HealthcareServices
OrganizationAffiliations
Organizations
Patients
PractitionerRoles
Practitioners

The result of the loop-else for an empty array:

    Found nobody with that name.
```

### Special note on spacing

Through the samples above you might have noticed the use of the - (dash) character on and off. The dash in combination with tags take care of spacing. Using a dash at the beginning of a tag will eliminate spaces and newlines before that tag. Using a dash at the end will eliminate a newline after that tag. In most case you'll have to fiddle around with these to get the result you want. Especially with for-loops it can get a bit tricky. But don't worry, you'll get the hang of it eventually :p

Below some samples of use:

```yaml
Line before normal assign
{% assign x = "some value" %}
Line after normal assign

Line before dash in the beginning
{%- assign x = "some value" %}
Line after dash in the beginning

Line before dash at the end
{% assign x = "some value" -%}
Line after dash at the end

Line before dash on both ends
{%- assign x = "some value" -%}
Line after dash on both ends
```

The output of these commands:

```yaml
Line before normal assign

Line after normal assign

Line before dash in the beginning
Line after dash in the beginning

Line before dash at the end
Line after dash at the end

Line before dash on both endsLine after dash on both ends
```

## Filters

Filters change the output of a Liquid object or variable. They are used within double curly braces **{{ }}** and variable assignment, and are separated by a pipe character **|**. There are quite some filters defined in the standard language. A few important ones are in the sections below.

### String filters

* downcase / upcase - Makes each character in a string lowercase / uppercase.
* capitalize - Makes the first character of a string capitalized and converts the remaining characters to lowercase.
* replace - Replaces every occurrence of the first argument in a string with the second argument.
* strip_newlines - Removes any newline characters (line breaks) from a string.

See below how these can be used and the output.

```yaml
{% comment -%}Downcase{%- endcomment -%}
Downcase of 'EXAMPLE': {{ "EXAMPLE" | downcase }}

{% comment -%}Upcase{%- endcomment -%}
Upcase of 'example': {{ "example" | upcase }}

{% comment -%}Capitalize{%- endcomment -%}
Capitalize of 'pete johnson': {{ "pete johnson" | capitalize }}

{% comment -%}Replace{%- endcomment -%}
Replace all 'u' in 'bununu' with 'a': {{ "bununu" | replace: "u", "a" }}

{% comment -%}This sample is a bit more elaborate, as we need something with a newline. 
For more information see the explanation of Tags further down this document.{%- endcomment -%}
{% capture string_with_newlines -%}
Hello
Liquid
{%- endcapture -%}
{{ string_with_newlines | strip_newlines }}
```

### String- and Array filters

* first / last - Returns the first / last item of an array.
* size - Returns the number characters in a string or the number of items in an array.
* split - Divides a string into an array using the argument as a separator. split is commonly used to convert comma-separated items from a string to an array.

See below how these can be used and the output.

```yaml
{% comment -%}First{%- endcomment -%}
{% assign table = model.Tables | first -%}
First table name: {{ table.Name }}

{% comment -%}Last{%- endcomment -%}
{% assign table = model.Tables | last -%}
Last table name: {{ table.Name }}

{% comment -%}Size{%- endcomment -%}
Size of a string 'example text': {{ "example text" | size }}
Size of the Tables array: {{ model.Tables | size }}

{%- comment -%}Split{%- endcomment %}
{% assign teamWindmill = "Laurent, Jan, Carlos, Ibrahim, Bart, Isabel, Martin" | split: ", " %}
Team Windmill has {{ teamWindmill | size }} members:
{% for member in teamWindmill -%}
  {{ member }}
{% endfor -%}
```

The output for this part:

```yaml
First table name: Claims

Last table name: Practitioners

Size of a string 'example text': 12
Size of the Tables array: 7

Team Windmill has 7 members:
Laurent
Jan
Carlos
Ibrahim
Bart
Isabel
Martin
```

### Special focus: where filter

The **where** filter can be used on arrays to create a new array with only the objects with a given property value. In other words, this is a very simple where-clause.

Important to realize is that the result is always an array, even if nothing is returned or if just 1 item is found. If you expect 1 item, you still have to address the first item of the array. For this purpose the `first` filter can be used. You can combine where clauses like an 'AND' by adding more where filters.

See below how these can be used and the output.

```yaml
{% comment -%}First show patients for reference{%- endcomment %}
Patients:   
{% for patient in model.Patients.Records -%}
{{ patient.Id }} = {{ patient.Name }} ({{ patient.gender }})
{% endfor -%}

{% comment -%}Now show just the female patients{%- endcomment %}
{% assign females = model.Patients.Records | where: "gender", "F" -%}
Female Patients:
{% for female in females -%}
{{ female.Id }} = {{ female.Name }} ({{ female.gender }})
{% endfor -%}

{% comment -%}Select a single item (or none) from the collection{%- endcomment %}
{%- assign patient = model.Patients.Records | where: "Id", "1004" | first %}
Selected patient: {{ patient.Id }} = {{ patient.Name }} ({{ patient.gender }})
```

The output:

```yaml
Patients:   
1000 = Benjamin Morris (M)
1001 = Kimberly Hall (F)
1002 = Nicole Parker (F)
1003 = Heather Williams (F)
1004 = Michael Smith (M)
1005 = Edward Morgan (M)
1006 = Mia Anderson (F)
1007 = Ava Brown (F)
1008 = Paul Clark (M)
1009 = Harry Thompson (M)

Female Patients:
1001 = Kimberly Hall (F)
1002 = Nicole Parker (F)
1003 = Heather Williams (F)
1006 = Mia Anderson (F)
1007 = Ava Brown (F)

Selected patient: 1004 = Michael Smith (M)
```

### Other filters worth mentioning

* date - Converts a timestamp into another date format. To get the current time, pass the special word "now" (or "today") to date.
* url_encode - Converts any URL-unsafe characters in a string into percent-encoded characters.
* url_decode - Decodes a string that has been encoded as a URL or by url_encode.

See below how these can be used and the output.

```yaml
{%- comment -%}Date{%- endcomment -%}
Date for now with format: {{ "now" | date: "%Y-%m-%d %H:%M:%S" }}
Date for today with format: {{ "today" | date: "%Y-%m-%d %H:%M:%S" }}

{% comment -%}url_encode{%- endcomment -%}
URL encoding 'https://shopify.github.io/liquid/': {{ "https://shopify.github.io/liquid/" | url_encode }}
URL encoding 'Pete Johnson': {{ "Pete Johnson" | url_encode }}

{% comment -%}url_decode{%- endcomment -%}
{{ "%27Liquid%21%27+said+Martin" | url_decode }}
```

The output:

```yaml
Date for now with format: 2021-12-08 08:59:49
Date for today with format: 2021-12-08 08:59:49

URL encoding 'https://shopify.github.io/liquid/': https%3A%2F%2Fshopify.github.io%2Fliquid%2F
URL encoding 'Pete Johnson': Pete+Johnson

'Liquid!' said Martin
```
