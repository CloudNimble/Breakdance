using Flurl;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Breakdance.Blazor
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Inspired by https://github.com/bUnit-dev/bUnit/issues/73#issuecomment-597828532
    /// </remarks>
    public class TestableNavigationManager : NavigationManager
    {

        #region Private Members

        private string _baseUrl;

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string NavigationResult { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "<Pending>")]
        public TestableNavigationManager(string baseUrl)
        {
            _baseUrl = baseUrl;
            EnsureInitialized();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        protected sealed override void EnsureInitialized()
        {
            Initialize(_baseUrl, _baseUrl);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="forceLoad"></param>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            NavigationResult = uri;
            Uri = _baseUrl.AppendPathSegment(uri);
        }

        #endregion

    }

}
