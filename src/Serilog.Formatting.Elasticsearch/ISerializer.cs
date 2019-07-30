namespace Serilog.Formatting.Elasticsearch
{
    /// <summary>
    ///     Defines a method to serialize custom value to string
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        ///     Serializes object to string
        /// </summary>
        /// <param name="value">Object to serialization</param>
        /// <returns>String representation of object</returns>
        string SerializeToString(object value);
    }
}