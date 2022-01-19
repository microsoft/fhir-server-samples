namespace FhirIngestion.Tools.Converter.Services
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Converter.Models;

    /// <summary>
    /// The service to read the liquid templates from disc.
    /// </summary>
    public class TemplateService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateService"/> class.
        /// </summary>
        public TemplateService()
        {
            Templates = new List<LiquidTemplate>();
        }

        /// <summary>
        /// Delegate for notify caller which file is being read.
        /// </summary>
        /// <param name="filename">Filename being read.</param>
        public delegate void FileNotifierDelegate(string filename);

        /// <summary>
        /// Public event being fired when a file is read.
        /// </summary>
        public event FileNotifierDelegate ReadingFile;

        /// <summary>
        /// Gets the templates.
        /// </summary>
        public List<LiquidTemplate> Templates { get; private set; }

        /// <summary>
        /// Read all the liquid files from the given folder and hold them in memory.
        /// </summary>
        /// <param name="folder">Folder that contains liquid-templates.</param>
        public void ReadAllTemplates(string folder)
        {
            Precondition.NotNull(folder);

            Templates.Clear();
            string[] files = Directory.GetFiles(folder, "*.liquid");
            foreach (string file in files)
            {
                ReadingFile?.Invoke(file);

                string content = File.ReadAllText(file);
                LiquidTemplate template = new LiquidTemplate
                {
                    Filename = Path.GetFileName(file),
                    Name = Path.GetFileNameWithoutExtension(file),
                    Content = content,
                };
                Templates.Add(template);
            }
        }

        /// <summary>
        /// Verbose output of templates collection.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public void WriteVerbose()
        {
            MessageHelper.Verbose($"Read {Templates.Count} templates:");
            foreach (LiquidTemplate template in Templates)
            {
                MessageHelper.Verbose($"\t{template.Name} ({template.Filename})");
            }
        }
    }
}
