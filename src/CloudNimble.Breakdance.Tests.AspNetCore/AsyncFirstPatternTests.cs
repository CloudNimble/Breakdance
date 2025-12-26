using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests to ensure the async-first pattern is properly implemented and prevents infinite recursion.
    /// These tests verify that:
    /// 1. Sync methods delegate to async methods (not the reverse)
    /// 2. No infinite recursion occurs in the method chain
    /// 3. Base methods are called exactly once
    /// </summary>
    [TestClass]
    public class AsyncFirstPatternTests
    {

        #region Test Helper Class

        /// <summary>
        /// A test-specific derived class that tracks method call counts to verify the async-first pattern.
        /// </summary>
        private class CallTrackingTestBase : AspNetCoreBreakdanceTestBase
        {
            public int TestSetupCallCount { get; private set; }
            public int TestSetupAsyncCallCount { get; private set; }
            public int AssemblySetupCallCount { get; private set; }
            public int AssemblySetupAsyncCallCount { get; private set; }

            public CallTrackingTestBase()
            {
                // Configure minimal MVC so EnsureTestServerAsync doesn't throw
                AddMinimalMvc();
            }

            public override void TestSetup()
            {
                TestSetupCallCount++;
                base.TestSetup();
            }

            public override async Task TestSetupAsync()
            {
                TestSetupAsyncCallCount++;
                await base.TestSetupAsync().ConfigureAwait(false);
            }

            public override void AssemblySetup()
            {
                AssemblySetupCallCount++;
                base.AssemblySetup();
            }

            public override async Task AssemblySetupAsync()
            {
                AssemblySetupAsyncCallCount++;
                await base.AssemblySetupAsync().ConfigureAwait(false);
            }

            /// <summary>
            /// Resets all call counts for a fresh test.
            /// </summary>
            public void ResetCallCounts()
            {
                TestSetupCallCount = 0;
                TestSetupAsyncCallCount = 0;
                AssemblySetupCallCount = 0;
                AssemblySetupAsyncCallCount = 0;
            }
        }

        #endregion

        #region TestSetup Tests

        /// <summary>
        /// Verifies that calling TestSetup() synchronously completes without causing a stack overflow.
        /// </summary>
        [TestMethod]
        public void TestSetup_DoesNotCauseInfiniteRecursion()
        {
            var tracker = new CallTrackingTestBase();

            // Act - this should complete without stack overflow
            Action act = () => tracker.TestSetup();

            // Assert - should not throw (especially not StackOverflowException)
            act.Should().NotThrow("calling TestSetup should not cause infinite recursion");

            tracker.TestTearDown();
        }

        /// <summary>
        /// Verifies that calling TestSetupAsync() completes without causing infinite recursion.
        /// </summary>
        [TestMethod]
        public async Task TestSetupAsync_DoesNotCauseInfiniteRecursion()
        {
            var tracker = new CallTrackingTestBase();

            // Act - this should complete without stack overflow
            Func<Task> act = async () => await tracker.TestSetupAsync();

            // Assert - should not throw
            await act.Should().NotThrowAsync("calling TestSetupAsync should not cause infinite recursion");

            tracker.TestTearDown();
        }

        /// <summary>
        /// Verifies that TestSetup() delegates to TestSetupAsync() (async-first pattern).
        /// The async method should be called exactly once when the sync method is invoked.
        /// </summary>
        [TestMethod]
        public void TestSetup_DelegatesToAsyncMethod()
        {
            var tracker = new CallTrackingTestBase();

            // Act
            tracker.TestSetup();

            // Assert - sync should delegate to async
            tracker.TestSetupCallCount.Should().Be(1, "TestSetup should be called once");
            tracker.TestSetupAsyncCallCount.Should().Be(1, "TestSetup should delegate to TestSetupAsync exactly once");

            tracker.TestTearDown();
        }

        /// <summary>
        /// Verifies that TestSetupAsync() is called exactly once (not duplicated through call chain).
        /// </summary>
        [TestMethod]
        public async Task TestSetupAsync_CallsBaseMethodExactlyOnce()
        {
            var tracker = new CallTrackingTestBase();

            // Act
            await tracker.TestSetupAsync();

            // Assert
            tracker.TestSetupAsyncCallCount.Should().Be(1, "TestSetupAsync should be called exactly once");
            tracker.TestSetupCallCount.Should().Be(0, "TestSetup sync method should not be called when using async directly");

            tracker.TestTearDown();
        }

        #endregion

        #region AssemblySetup Tests

        /// <summary>
        /// Verifies that calling AssemblySetup() synchronously completes without causing a stack overflow.
        /// </summary>
        [TestMethod]
        public void AssemblySetup_DoesNotCauseInfiniteRecursion()
        {
            var tracker = new CallTrackingTestBase();

            // Act - this should complete without stack overflow
            Action act = () => tracker.AssemblySetup();

            // Assert - should not throw
            act.Should().NotThrow("calling AssemblySetup should not cause infinite recursion");

            tracker.TestTearDown();
        }

        /// <summary>
        /// Verifies that calling AssemblySetupAsync() completes without causing infinite recursion.
        /// </summary>
        [TestMethod]
        public async Task AssemblySetupAsync_DoesNotCauseInfiniteRecursion()
        {
            var tracker = new CallTrackingTestBase();

            // Act - this should complete without stack overflow
            Func<Task> act = async () => await tracker.AssemblySetupAsync();

            // Assert - should not throw
            await act.Should().NotThrowAsync("calling AssemblySetupAsync should not cause infinite recursion");

            tracker.TestTearDown();
        }

        /// <summary>
        /// Verifies that AssemblySetup() delegates to AssemblySetupAsync() (async-first pattern).
        /// </summary>
        [TestMethod]
        public void AssemblySetup_DelegatesToAsyncMethod()
        {
            var tracker = new CallTrackingTestBase();

            // Act
            tracker.AssemblySetup();

            // Assert - sync should delegate to async
            tracker.AssemblySetupCallCount.Should().Be(1, "AssemblySetup should be called once");
            tracker.AssemblySetupAsyncCallCount.Should().Be(1, "AssemblySetup should delegate to AssemblySetupAsync exactly once");

            tracker.TestTearDown();
        }

        /// <summary>
        /// Verifies that AssemblySetupAsync() is called exactly once (not duplicated through call chain).
        /// </summary>
        [TestMethod]
        public async Task AssemblySetupAsync_CallsBaseMethodExactlyOnce()
        {
            var tracker = new CallTrackingTestBase();

            // Act
            await tracker.AssemblySetupAsync();

            // Assert
            tracker.AssemblySetupAsyncCallCount.Should().Be(1, "AssemblySetupAsync should be called exactly once");
            tracker.AssemblySetupCallCount.Should().Be(0, "AssemblySetup sync method should not be called when using async directly");

            tracker.TestTearDown();
        }

        #endregion

        #region Multiple Calls Tests

        /// <summary>
        /// Verifies that multiple sequential calls to TestSetup() don't compound the call counts unexpectedly.
        /// Each call should result in exactly one call to TestSetupAsync.
        /// </summary>
        [TestMethod]
        public void TestSetup_MultipleCalls_MaintainsCorrectCallRatio()
        {
            var tracker = new CallTrackingTestBase();

            // Act - call TestSetup twice
            tracker.TestSetup();
            tracker.TestTearDown();

            // Reset for second iteration (need new TestServer)
            tracker.ResetCallCounts();
            tracker.TestHostBuilder = new Microsoft.Extensions.Hosting.HostBuilder();
            tracker.AddMinimalMvc();

            tracker.TestSetup();

            // Assert - each sync call should delegate to async exactly once
            tracker.TestSetupCallCount.Should().Be(1, "second TestSetup call tracked");
            tracker.TestSetupAsyncCallCount.Should().Be(1, "second TestSetup should delegate to TestSetupAsync once");

            tracker.TestTearDown();
        }

        #endregion

    }

}
