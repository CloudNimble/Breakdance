namespace AdvancedREI.Breakdance.Tests.SampleApis
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

        public static event SomeEventHandler SomeEvent;

        #endregion

        #region Constructors

        static SomeStaticClass()
        {
            Name = "Testing 123";
        }

        #endregion

    }

}