namespace FhirIngestion.Tools.Converter.Services
{
    using System;
    using FhirIngestion.Tools.Converter.Exceptions;
    using FhirIngestion.Tools.Converter.FluidHelpers;
    using FhirIngestion.Tools.Converter.Models;
    using Fluid;
    using Microsoft.Extensions.FileProviders;

    /// <summary>
    /// The service to process a liquid template with the provided content.
    /// </summary>
    public class ParserService
    {
        /// <summary>
        /// Renders a string as a template.
        /// </summary>
        /// <param name="data">Data model to provide to the template.</param>
        /// <param name="templateContent">The template content.</param>
        /// <param name="rootFolder">Root folder to use for includes (optional).</param>
        /// <returns>A rendered template.</returns>
        public string Render(object data, string templateContent, string rootFolder = null)
        {
            var parser = new FluidParser();

            if (string.IsNullOrEmpty(templateContent))
            {
                return string.Empty;
            }

            // validating the template first
            if (parser.TryParse(templateContent, out IFluidTemplate template, out string error))
            {
                // now do the actual parsing
                TemplateOptions options = new TemplateOptions();
                options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();

                if (rootFolder != null)
                {
                    // add file provider for includes
                    options.FileProvider = new PhysicalFileProvider(rootFolder);
                }

                // add necessary object converters
                options.ValueConverters.Add(o => o is Model m ? new ModelObjectConverter(m) : null);
                options.ValueConverters.Add(o => o is Tables ts ? new TablesObjectConverter(ts) : null);
                options.ValueConverters.Add(o => o is Table t ? new TableObjectConverter(t) : null);
                options.ValueConverters.Add(o => o is Record r ? new RecordObjectConverter(r) : null);
                options.ValueConverters.Add(o => o is Fields fs ? new FieldsObjectConverter(fs) : null);

                var ctx = new TemplateContext(new { model = data }, options, true);
                try
                {
                    return template.Render(ctx);
                }
                catch (Exception ex)
                {
                    throw new ParserException(ex.Message, ex);
                }
            }
            else
            {
                throw new ParserException($"Parse error: {error}");
            }
        }
    }
}
