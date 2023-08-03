namespace SIL.Machine.AspNetCore.Utils;

public static class AutoToString
{
    //Helper functions for debugging and error printing data classes

    /// <summary>
    /// Renders the object and its properties as a string using introspection to a max depth of 50.
    /// </summary>
    /// <param name="o">Object to be printed</param>
    /// <returns>A recursively generated string representation of the object</returns>
    public static string Stringify(object? o)
    {
        //If null, return "null"
        if (o is null)
            return "null";
        //If not a "primitive" type, append type to accumulator and begin recursing
        if (!o!.GetType().IsValueType && o.GetType() != typeof(string))
        {
            string soFar = "(" + o!.GetType().Name + ")";
            return Stringify(o, soFar, 1);
        }
        //Otherwise, just use the built in toString
        return o.ToString()!;
    }

    private static string Stringify(object? o, string soFar = "", int tabDepth = 0, int itemIndex = 0)
    {
        //If more than 50 layers deep in the properties structure, terminate; prevents memory issues
        if (tabDepth > 50)
            return soFar;
        //If null, append `null` to soFar (accumulator)
        if (o is null)
            return soFar + (itemIndex > 0 ? "\n" + new string('\t', tabDepth) : " ") + "null";
        //If "primitive" type value, just append toString value
        if (o!.GetType().IsValueType || o.GetType() == typeof(string))
        {
            var value = o;
            //If it's a value of an item in a list-like structure, properly tab
            return soFar + (itemIndex > 0 ? "\n" + new string('\t', tabDepth) : " ") + value;
        }
        //If it's an item in a list, add index and type of item
        if (itemIndex > 0)
            soFar += "\n" + new string('\t', tabDepth - 1) + "(" + o.GetType().Name + "@Index" + itemIndex + ")";

        //Iterate through properties
        foreach (var property in o.GetType().GetProperties())
        {
            //Many objects have a count attribute that is redundant to list
            if (property.Name == "Count")
                continue;
            //If this is a property with indexed parameters, iterate through them and append properly incrementing `itemIndex`
            if (property.GetIndexParameters().Count() > 0)
            {
                foreach (var ele in property.GetIndexParameters())
                {
                    int index = 0;
                    try
                    {
                        while (true)
                        {
                            var next_obj = property.GetValue(o, new object[] { index });
                            soFar = Stringify(
                                next_obj,
                                soFar: soFar + "\n" + new string('\t', tabDepth) + (index == 0 ? "[" : ""),
                                tabDepth: tabDepth + 1,
                                itemIndex: index + 1
                            );
                            index++; //separately increment in case exception is thrown in inner GetAuto...
                        }
                    }
                    catch
                    {
                        //When the end of the item list has been hit or an unrenederable element has been hit, close with a `]` properly tabbed
                        soFar += "\n" + new string('\t', tabDepth) + (index == 0 ? "" : "]");
                    }
                }
            }
            //Otherwise, there are no index parameters for property
            else
            {
                soFar += "\n" + new string('\t', tabDepth) + property.Name + ":";
                soFar = Stringify(property.GetValue(o), soFar: soFar, tabDepth: tabDepth + 1);
            }
        }
        return soFar;
    }
}
