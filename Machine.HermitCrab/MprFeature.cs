namespace SIL.Machine.HermitCrab
{
    /// <summary>
    /// This class represents a morphological/phonological rule feature. It is used to restrict
    /// the application of rules for exception cases.
    /// </summary>
    public class MprFeature
    {
    	public string Name { get; set; }

    	/// <summary>
    	/// Gets or sets the MPR feature group.
    	/// </summary>
    	/// <value>The group.</value>
    	public MprFeatureGroup Group { get; internal set; }

	    public override string ToString()
	    {
		    return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
	    }
    }
}
