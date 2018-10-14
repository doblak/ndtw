namespace NDtw.Examples.Infrastructure.MultiSelect
{
    /// <summary>
    /// Converts items in the Master list to Items in the target list, and back again.
    /// </summary>
    /// <remarks>  
    /// Thanks to Samuel Jack for this code: http://blog.functionalfun.net/2009/02/how-to-databind-to-selecteditems.html
    /// </remarks>
    public interface IListItemConverter
    {
        /// <summary>
        /// Converts the specified master list item.
        /// </summary>
        /// <param name="masterListItem">The master list item.</param>
        /// <returns>The result of the conversion.</returns>
        object Convert(object masterListItem);
        /// My changes
        /// <summary>
        /// Converts the specified target list item.
        /// </summary>
        /// <param name="targetListItem">The target list item.</param>
        /// <returns>The result of the conversion.</returns>
        object ConvertBack(object targetListItem);
    }
}
