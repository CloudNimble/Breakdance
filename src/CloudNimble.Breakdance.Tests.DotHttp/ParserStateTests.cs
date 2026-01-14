using System;
using CloudNimble.Breakdance.DotHttp;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="ParserState"/> enum.
    /// </summary>
    [TestClass]
    public class ParserStateTests
    {

        #region Value Tests

        [TestMethod]
        public void ParserState_Start_HasCorrectValue()
        {
            ParserState.Start.Should().Be((ParserState)0);
        }

        [TestMethod]
        public void ParserState_InHeaders_HasCorrectValue()
        {
            ParserState.InHeaders.Should().Be((ParserState)1);
        }

        [TestMethod]
        public void ParserState_InBody_HasCorrectValue()
        {
            ParserState.InBody.Should().Be((ParserState)2);
        }

        #endregion

        #region Members Tests

        [TestMethod]
        public void ParserState_HasExpectedMembers()
        {
            var values = (ParserState[])Enum.GetValues(typeof(ParserState));

            values.Should().HaveCount(3);
            values.Should().Contain(ParserState.Start);
            values.Should().Contain(ParserState.InHeaders);
            values.Should().Contain(ParserState.InBody);
        }

        #endregion

    }

}
