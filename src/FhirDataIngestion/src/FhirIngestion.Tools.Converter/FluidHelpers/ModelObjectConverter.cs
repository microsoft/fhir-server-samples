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
    /// Fluid Object Converter for <see cref="Model"/> class.
    /// </summary>
    public class ModelObjectConverter : ObjectValueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectConverter"/> class.
        /// </summary>
        /// <param name="value"><see cref="Model"/> object.</param>
        public ModelObjectConverter(object value)
            : base(value)
        {
        }

        /// <summary>
        /// Get a property on the <see cref="Model"/>. If it's not a property on the class
        /// we'll use the name as an index to the <see cref="Tables"/> collection.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="context">Template context.</param>
        /// <returns><see cref="FluidValue"/> object.</returns>
        public override ValueTask<FluidValue> GetValueAsync(string name, TemplateContext context)
        {
            Precondition.NotNull(context);
            Precondition.NotNull(name);

            Model obj = Value as Model;
            if (name.Equals(nameof(Model.Tables), System.StringComparison.OrdinalIgnoreCase))
            {
                return Create(obj.Tables, context.Options);
            }

            // otherwise we'll use the property name as index to the tables collection.
            return Create(obj[name], context.Options);
        }

        /// <summary>
        /// Handle an index on this class which is used as index to the Tables collection.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <param name="context">Template context.</param>
        /// <returns>A <see cref="Table"/> with the given name/index.</returns>
        public override ValueTask<FluidValue> GetIndexAsync(FluidValue index, TemplateContext context)
        {
            Precondition.NotNull(index);
            Precondition.NotNull(context);

            Model obj = Value as Model;
            if (index.ToNumberValue().ToString(CultureInfo.InvariantCulture) == index.ToStringValue())
            {
                return Create(obj.Tables[(int)index.ToNumberValue()], context.Options);
            }
            else
            {
                return Create(obj.Tables[index.ToStringValue()], context.Options);
            }
        }
    }
}
