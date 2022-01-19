namespace FhirIngestion.Tools.Converter.FluidHelpers
{
    using System.Globalization;
    using System.Reflection;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Converter.Models;
    using Fluid;
    using Fluid.Values;

    /// <summary>
    /// Fluid Object Converter for <see cref="Record"/> class.
    /// </summary>
    public class RecordObjectConverter : ObjectValueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecordObjectConverter"/> class.
        /// </summary>
        /// <param name="value"><see cref="Record"/> object.</param>
        public RecordObjectConverter(object value)
            : base(value)
        {
        }

        /// <summary>
        /// Get a property on the <see cref="Record"/>. If it's not a property on the class
        /// we'll use the name as an index to the <see cref="Fields"/> collection.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="context">Template context.</param>
        /// <returns><see cref="FluidValue"/> object.</returns>
        public override ValueTask<FluidValue> GetValueAsync(string name, TemplateContext context)
        {
            Precondition.NotNull(name);
            Precondition.NotNull(context);

            Record obj = Value as Record;
            if (name.Equals(nameof(Record.Table), System.StringComparison.OrdinalIgnoreCase))
            {
                return Create(obj.Table, context.Options);
            }

            if (name.Equals(nameof(Record.Fields), System.StringComparison.OrdinalIgnoreCase))
            {
                return Create(obj.Fields, context.Options);
            }

            // otherwise we'll use the property name as index to the fields collection.
            return Create(obj.Fields[name], context.Options);
        }

        /// <summary>
        /// Handle an index on this class.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <param name="context">Template context.</param>
        /// <returns>The value of the <see cref="Field"/> with the given name.</returns>
        public override ValueTask<FluidValue> GetIndexAsync(FluidValue index, TemplateContext context)
        {
            Precondition.NotNull(index);
            Precondition.NotNull(context);

            Record obj = Value as Record;
            if (index.ToNumberValue().ToString(CultureInfo.InvariantCulture) == index.ToStringValue())
            {
                return Create(obj.Fields[(int)index.ToNumberValue()], context.Options);
            }
            else
            {
                return Create(obj.Fields[index.ToStringValue()], context.Options);
            }
        }
    }
}
