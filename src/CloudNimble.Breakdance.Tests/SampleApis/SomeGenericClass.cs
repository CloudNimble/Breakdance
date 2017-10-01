namespace CloudNimble.Breakdance.Tests.SampleApis
{

    /// <summary>
    /// 
    /// </summary>
    public class SomeGenericClass<T> where T : class
    {

        public const string YoMama = "Yo Mama!";

        #region Properties

        public static string Name { get; private set; }

        public static T Value { get; private set; }

        #endregion

        #region Constructors

        public SomeGenericClass()
        {
            Name = "Testing 123";
        }

        #endregion

        #region Public Methods

        public override string ToString() => Name;

        #endregion

    }

}