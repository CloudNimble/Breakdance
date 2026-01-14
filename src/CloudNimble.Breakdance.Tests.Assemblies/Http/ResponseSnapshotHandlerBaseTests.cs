using CloudNimble.Breakdance.Assemblies.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CloudNimble.Breakdance.Tests.Assemblies.Http
{

    /// <summary>
    /// Tests the functionality of the <see cref="ResponseSnapshotHandlerBase"/>.
    /// </summary>
    [TestClass]
    public class ResponseSnapshotHandlerBaseTests
    {

        #region Properties

        /// <summary>
        /// Root directory for storing response snapshot files.
        /// </summary>
        internal static string ResponseSnapshotsPath = "..\\..\\..\\..\\CloudNimble.Breakdance.Tests.Assemblies\\ResponseFiles";

        /// <summary>
        /// Provides a set of URIs for testing functionality of the <see cref="ResponseSnapshotHandlerBase"/>.
        /// </summary>
        /// <remarks>
        /// PLEASE NOTE: Based on your installation folder and your operating system, you may end up with a file path that
        ///              exceeds the allowable maximum for your operating system. If that is the case, you can choose to comment
        ///              out the longer lines in the data set below or modify the folder specified in the <see cref="ResponseSnapshotsPath"/>
        ///              variable above to shorten the path.
        /// </remarks>
        internal static IEnumerable<object[]> GetPathsAndTestUris =>
            new List<object[]>
            {
                new object[] { "application/json", "test.somewebsite.io\\v2\\TestCaseStages\\expand=TestCase\\expand=Test\\expand=Offering\\expand=CompanyTemplateDepartmentCaseStageType", "orderby=TestCaseTestOfferingCompanyCompanyNameT", "https://test.somewebsite.io/v2/TestCaseStages?$expand=TestCase($expand=Test($expand=Offering($expand=Company)),Template),Department,CaseStageType&$orderby=TestCase/Test/Offering/Company/CompanyName,TestCase/Test/Offering/DisplayName,TestCase/Test/DisplayName,SortOrder,PeerSortOrder" },
                new object[] { "application/json", "services.odata.org", "root", "https://services.odata.org" },
                new object[] { "text/xml",         "services.odata.org", "metadata", "https://services.odata.org/$metadata" },
                new object[] { "application/json", "services.odata.org\\Entity", "filter=query", "https://services.odata.org/Entity?$filter=query" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People", "root", "https://services.odata.org/TripPinRESTierService/People" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airports\\'KSFO'\\Name", "root", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Name" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airports\\'KSFO'\\Location\\Address", "root", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Location/Address" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airports\\'KSFO'\\Name", "value", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Name/$value" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'\\Gender", "value", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/Gender/$value" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airports\\'KSFO'\\Location", "root", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Location" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'\\AddressInfo", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/AddressInfo" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People", "filter=FirstName_eq_'Scott'", "https://services.odata.org/TripPinRESTierService/People?$filter=FirstName eq 'Scott'" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airports", "filter=containsLocationAddress_'San_Francisco'", "https://services.odata.org/TripPinRESTierService/Airports?$filter=contains(Location/Address, 'San Francisco')" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airports", "select=Name_IcaoCode", "https://services.odata.org/TripPinRESTierService/Airports?$select=Name, IcaoCode" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'scottketchum'\\Trips", "orderby=EndsAt_desc", "https://services.odata.org/TripPinRESTierService/People('scottketchum')/Trips?$orderby=EndsAt desc" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People", "top=2", "https://services.odata.org/TripPinRESTierService/People?$top=2" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People", "skip=18", "https://services.odata.org/TripPinRESTierService/People?$skip=18" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People", "count", "https://services.odata.org/TripPinRESTierService/People/$count" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Me\\Friends", "filter=FriendsanyffFirstName_eq_'Scott'", "https://services.odata.org/TripPinRESTierService/Me/Friends?$filter=Friends/any(f:f/FirstName eq 'Scott')" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'keithpinckney'\\expand=Trips", "root", "https://services.odata.org/TripPinRESTierService/People('keithpinckney')?$expand=Trips" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'\\expand=Trips", "top=1", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')?$expand=Trips($top=1)" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'\\expand=Trips", "select=TripId_Name", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')?$expand=Trips($select=TripId, Name)" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'\\expand=Trips", "filter=Name_eq_'Trip_in_US'", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')?$expand=Trips($filter=Name eq 'Trip in US')" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\GetNearestAirport\\lat_=_33_lon_=_-118", "root", "https://services.odata.org/TripPinRESTierService/GetNearestAirport(lat = 33, lon = -118)" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\ResetDataSource", "root", "https://services.odata.org/TripPinRESTierService/ResetDataSource" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airlines", "root", "https://services.odata.org/TripPinRESTierService/Airlines" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\Airlines\\'AA'", "root", "https://services.odata.org/TripPinRESTierService/Airlines('AA')" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People", "filter=Gender_eq_Microsoft.OData.Service.Sample.TrippinInMemory.Models.PersonGender'Female'", "https://services.odata.org/TripPinRESTierService/People?$filter=Gender eq Microsoft.OData.Service.Sample.TrippinInMemory.Models.PersonGender'Female'" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'\\Trips\\0\\Microsoft.OData.Service.Sample.TrippinInMemory.Models.GetInvolvedPeople", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/Trips(0)/Microsoft.OData.Service.Sample.TrippinInMemory.Models.GetInvolvedPeople" },
                new object[] { "application/json", "services.odata.org\\TripPinRESTierService\\People\\'russellwhyte'\\Microsoft.OData.Service.Sample.TrippinInMemory.Models.ShareTrip", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/Microsoft.OData.Service.Sample.TrippinInMemory.Models.ShareTrip" },
            };

        #endregion

        #region Tests

        /// <summary>
        /// Tests that the <see cref="ResponseSnapshotHandlerBase"/> can correctly parse all of the records provided in <see cref="GetPathsAndTestUris"/>.
        /// </summary>
        /// <param name="mediaType">The media type for the request.</param>
        /// <param name="directoryPath">The expected directory path.</param>
        /// <param name="fileName">The expected file name.</param>
        /// <param name="requestUri">The request URI to parse.</param>
        [TestMethod]
#pragma warning disable MSTEST0018 // DynamicData should be valid
        [DynamicData(nameof(GetPathsAndTestUris))]
#pragma warning restore MSTEST0018 // DynamicData should be valid
        public void GetPathInfo_CanParse_Uris(string mediaType, string directoryPath, string fileName, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));

            var (DirectoryPath, FilePath) = ResponseSnapshotHandlerBase.GetPathInfo(request, ResponseSnapshotsPath);

            Path.GetFileName(FilePath).IndexOfAny(Path.GetInvalidFileNameChars()).Should().BeLessThan(0);
            DirectoryPath.Should().Be(directoryPath);
            FilePath.Should().Be($"{fileName}{ResponseSnapshotHandlerBase.GetFileExtensionString(request)}");
        }

        #endregion

    }

}
