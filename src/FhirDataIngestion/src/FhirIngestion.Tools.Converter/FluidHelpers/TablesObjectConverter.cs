namespace FhirIngestion.Tools.Converter.FluidHelpers
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Converter.Models;
    using Fluid;
    using Fluid.Values;

    /// <summary>
    /// Fluid Object Converter for <see cref="Tables"/> class.
    /// </summary>
    public class TablesObjectConverter : ObjectValueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TablesObjectConverter"/> class.
        /// </summary>
        /// <param name="value"><see cref="Tables"/> object.</param>
        public TablesObjectConverter(object value)
            : base(value)
        {
        }

        /// <inheritdoc/>
        public override ValueTask<FluidValue> GetValueAsync(string name, TemplateContext context)
        {
            Precondition.NotNull(name);
            Precondition.NotNull(context);

            Tables obj = Value as Tables;
            switch (name)
            {
                case "size":
                    return Create(obj.Count, context.Options);
                case "first":
                    return Create(obj.FirstOrDefault(), context.Options);
                case "last":
                    return Create(obj.LastOrDefault(), context.Options);
            }

            // otherwise we'll return a null value.
            return Create(null, context.Options);
        }

        /// <summary>
        /// Handle an index on this class, which is the element number or the table name.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <param name="context">Template context.</param>
        /// <returns>A <see cref="Table"/> with the given name.</returns>
        public override ValueTask<FluidValue> GetIndexAsync(FluidValue index, TemplateContext context)
        {
            Precondition.NotNull(index);
            Precondition.NotNull(context);

            Tables obj = Value as Tables;
            if (index.ToNumberValue().ToString(CultureInfo.InvariantCulture) == index.ToStringValue())
            {
                // it's a number
                return Create(obj[(int)index.ToNumberValue()], context.Options);
            }
            else
            {
                // it's a string, so the table name
                return Create(obj[index.ToStringValue()], context.Options);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<FluidValue> Enumerate(TemplateContext context)
        {
            Precondition.NotNull(context);

            Tables tables = Value as Tables;
            List<FluidValue> list = new List<FluidValue>();
            foreach (Table table in tables)
            {
                list.Add(FluidValue.Create(table, context.Options));
            }

            return list.ToArray();
        }
    }
}
