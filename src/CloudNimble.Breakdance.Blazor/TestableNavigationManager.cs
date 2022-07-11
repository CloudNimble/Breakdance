using Flurl;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace CloudNimble.Breakdance.Blazor
{

    /// <summary>
    /// A <see cref="NavigationManager"/> instance suitable for local unit tests.
    /// </summary>
    /// <remarks>
    /// Inspired by https://github.com/bUnit-dev/bUnit/issues/73#issuecomment-597828532
    /// </remarks>
    public class TestableNavigationManager : NavigationManager
    {

        #region Private Members

        private readonly string _baseUrl;

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
        public TestableNavigationManager()
        {
            _baseUrl = "https://localhost/";
            EnsureInitialized();
        }

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
        /// <remarks>
        /// This changed to <see cref="Url.Combine" /> due to the issue with <see cref="HttpClient.BaseAddress"/> requiring trailing slashes.
        /// See <see href="https://stackoverflow.com/a/23438417" /> for more details.
        /// </remarks>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            NavigationResult = uri;
            Uri = Url.Combine(_baseUrl, uri);
        }

        #endregion

    }

}
