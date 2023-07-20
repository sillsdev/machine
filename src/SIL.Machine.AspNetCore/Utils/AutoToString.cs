namespace SIL.Machine.AspNetCore.Utils;

public static class AutoToString
{
    //Helper functions for debugging and error printing data classes

    /// <summary>
    /// Renders the object and its properties as a string using introspection.
    /// </summary>
    /// <param name="o">Object to be printed</param>
    /// <returns>A recursively generated string representation of the object</returns>
    public static string Stringify(object? o)
    {
        if (o is null)
            return "null";
        if (!o!.GetType().IsValueType && o.GetType() != typeof(string))
        {
            string soFar = "(" + o!.GetType().Name + ")";
            return Stringify(o, soFar, 1);
        }
        return o is null ? "" : o.ToString()!;
    }

    private static string Stringify(object? o, string soFar = "", int tabDepth = 0, int itemIndex = 0)
    {
        if (tabDepth > 50)
            return soFar;
        if (o is null)
            return soFar + (itemIndex > 0 ? "\n" + new string('\t', tabDepth) : " ") + "null";
        if (o!.GetType().IsValueType || o.GetType() == typeof(string))
        {
            var value = o;
            return soFar + (itemIndex > 0 ? "\n" + new string('\t', tabDepth) : " ") + value;
        }
        if (itemIndex > 0)
            soFar += "\n" + new string('\t', tabDepth - 1) + "(" + o.GetType().Name + "@Index" + itemIndex + ")";
        foreach (var property in o.GetType().GetProperties())
        {
            if (property.Name == "Count")
                continue;
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
                        soFar += "\n" + new string('\t', tabDepth) + (index == 0 ? "" : "]");
                        // + (soFar.Count(c => c == '[') > soFar.Count(c => c == ']') ? "]" : "");
                    }
                }
            }
            else
            {
                soFar += "\n" + new string('\t', tabDepth) + property.Name + ":";
                soFar = Stringify(property.GetValue(o), soFar: soFar, tabDepth: tabDepth + 1);
            }
        }
        return soFar;
    }
}
