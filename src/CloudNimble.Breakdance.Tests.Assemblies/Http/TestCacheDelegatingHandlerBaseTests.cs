using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Assemblies.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Assemblies.Http
{

    /// <summary>
    /// Tests the functionality of the <see cref="StaticFileResponseHttpMessageHandler"/>.
    /// </summary>
    [TestClass]
    public class TestCacheDelegatingHandlerBaseTests
    {

        /// <summary>
        /// Root directory for storing response files.
        /// </summary>
        internal static string ResponseFilesPath = "..\\..\\..\\ResponseFiles";

        internal static IEnumerable<object[]> GetPathsAndTestUris =>
            new List<object[]>
            {
                new object[] { "services.odata.org", "root", "https://services.odata.org" },
                new object[] { "services.odata.org", "$metadata", "https://services.odata.org/$metadata" },
                new object[] { "services.odata.org\\Entity", "$filter=query", "https://services.odata.org/Entity?$filter=query" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People", "root", "https://services.odata.org/TripPinRESTierService/People" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airports\\('KSFO')\\Name", "root", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Name" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airports\\('KSFO')\\Location\\Address", "root", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Location/Address" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airports\\('KSFO')\\Name", "$value", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Name/$value" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')\\Gender", "$value", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/Gender/$value" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airports\\('KSFO')\\Location", "root", "https://services.odata.org/TripPinRESTierService/Airports('KSFO')/Location" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')\\AddressInfo", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/AddressInfo" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People", "$filter=FirstName_eq_'Scott'", "https://services.odata.org/TripPinRESTierService/People?$filter=FirstName eq 'Scott'" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airports", "$filter=contains(LocationAddress,_'San_Francisco')", "https://services.odata.org/TripPinRESTierService/Airports?$filter=contains(Location/Address, 'San Francisco')" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People", "$filter=Gender_eq_Microsoft.OData.Service.Sample.TrippinInMemory.Models.PersonGender'Female'", "https://services.odata.org/TripPinRESTierService/People?$filter=Gender eq Microsoft.OData.Service.Sample.TrippinInMemory.Models.PersonGender'Female'" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airports", "$select=Name,_IcaoCode", "https://services.odata.org/TripPinRESTierService/Airports?$select=Name, IcaoCode" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('scottketchum')\\Trips", "$orderby=EndsAt_desc", "https://services.odata.org/TripPinRESTierService/People('scottketchum')/Trips?$orderby=EndsAt desc" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People", "$top=2", "https://services.odata.org/TripPinRESTierService/People?$top=2" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People", "$skip=18", "https://services.odata.org/TripPinRESTierService/People?$skip=18" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People", "$count", "https://services.odata.org/TripPinRESTierService/People/$count" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Me\\Friends", "$filter=Friendsany(ffFirstName_eq_'Scott')", "https://services.odata.org/TripPinRESTierService/Me/Friends?$filter=Friends/any(f:f/FirstName eq 'Scott')" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('keithpinckney')", "$expand=Trips", "https://services.odata.org/TripPinRESTierService/People('keithpinckney')?$expand=Trips" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')", "$expand=Trips($top=1)", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')?$expand=Trips($top=1)" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')", "$expand=Trips($select=TripId,_Name)", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')?$expand=Trips($select=TripId, Name)" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')", "$expand=Trips($filter=Name_eq_'Trip_in_US')", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')?$expand=Trips($filter=Name eq 'Trip in US')" },
                new object[] { "services.odata.org\\TripPinRESTierService\\GetNearestAirport\\(lat_=_33,_lon_=_-118)", "root", "https://services.odata.org/TripPinRESTierService/GetNearestAirport(lat = 33, lon = -118)" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')\\Trips\\(0)\\Microsoft.OData.Service.Sample.TrippinInMemory.Models.GetInvolvedPeople", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/Trips(0)/Microsoft.OData.Service.Sample.TrippinInMemory.Models.GetInvolvedPeople" },
                new object[] { "services.odata.org\\TripPinRESTierService\\ResetDataSource", "root", "https://services.odata.org/TripPinRESTierService/ResetDataSource" },
                new object[] { "services.odata.org\\TripPinRESTierService\\People\\('russellwhyte')\\Microsoft.OData.Service.Sample.TrippinInMemory.Models.ShareTrip", "root", "https://services.odata.org/TripPinRESTierService/People('russellwhyte')/Microsoft.OData.Service.Sample.TrippinInMemory.Models.ShareTrip" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airlines", "root", "https://services.odata.org/TripPinRESTierService/Airlines" },
                new object[] { "services.odata.org\\TripPinRESTierService\\Airlines\\('AA')", "root", "https://services.odata.org/TripPinRESTierService/Airlines('AA')" },
            };

        [TestMethod]
        [DynamicData(nameof(GetPathsAndTestUris))]
        public void GetStaticFilePath_CanParse_Uris(string directoryPath, string fileName, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var (DirectoryPath, FilePath) = TestCacheDelegatingHandlerBase.GetStaticFilePath(request);
            Path.GetFileName(FilePath).IndexOfAny(Path.GetInvalidFileNameChars()).Should().BeLessThan(0);
            DirectoryPath.Should().Be(directoryPath);
            FilePath.Should().Be(fileName);
        }

    }
}
