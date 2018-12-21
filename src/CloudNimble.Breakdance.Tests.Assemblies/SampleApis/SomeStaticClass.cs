namespace CloudNimble.Breakdance.Tests.Assemblies.SampleApis
{

    /// <summary>
    /// 
    /// </summary>
    public static class SomeStaticClass
    {

        #region Properties

        public static string Name { get; }

        #endregion

        #region Events

        public delegate void SomeEventHandler(object sender, SomeEventArgs e);

#pragma warning disable CS0067
        public static event SomeEventHandler SomeEvent;
#pragma warning restore CS0067

        #endregion

        #region Constructors

        static SomeStaticClass()
        {
            Name = "Testing 123";
        }

        #endregion

    }

}