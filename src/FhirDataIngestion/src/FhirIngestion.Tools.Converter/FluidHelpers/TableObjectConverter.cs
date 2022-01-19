namespace FhirIngestion.Tools.Converter.FluidHelpers
{
    using System.Reflection;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Converter.Models;
    using Fluid;
    using Fluid.Values;

    /// <summary>
    /// Fluid Object Converter for <see cref="Table"/> class.
    /// </summary>
    public class TableObjectConverter : ObjectValueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableObjectConverter"/> class.
        /// </summary>
        /// <param name="value"><see cref="Table"/> object.</param>
        public TableObjectConverter(object value)
            : base(value)
        {
        }

        /// <summary>
        /// Get a property on the <see cref="Table"/>. If it's not a property on the class
        /// we'll return a null value.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="context">Template context.</param>
        /// <returns><see cref="FluidValue"/> object.</returns>
        public override ValueTask<FluidValue> GetValueAsync(string name, TemplateContext context)
        {
            Precondition.NotNull(name);
            Precondition.NotNull(context);

            Table obj = Value as Table;
            if (name.Equals(nameof(Table.Name), System.StringComparison.OrdinalIgnoreCase))
            {
                return Create(obj.Name, context.Options);
            }

            if (name.Equals(nameof(Table.FieldNames), System.StringComparison.OrdinalIgnoreCase))
            {
                return Create(obj.FieldNames, context.Options);
            }

            if (name.Equals(nameof(Table.Records), System.StringComparison.OrdinalIgnoreCase))
            {
                return Create(obj.Records, context.Options);
            }

            // otherwise we'll return a null value.
            return Create(null, context.Options);
        }

        /// <summary>
        /// Handle an index on this class, which is an index into the records collection.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <param name="context">Template context.</param>
        /// <returns>The <see cref="Record"/> on the given index.</returns>
        public override ValueTask<FluidValue> GetIndexAsync(FluidValue index, TemplateContext context)
        {
            Precondition.NotNull(index);
            Precondition.NotNull(context);

            Table obj = Value as Table;
            return Create(obj.Records[(int)index.ToNumberValue()], context.Options);
        }
    }
}
