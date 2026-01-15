export const MockVsReal = () => {
  const [animationsStarted, setAnimationsStarted] = React.useState(false);

  React.useEffect(() => {
    setAnimationsStarted(true);
  }, []);

  return (
    <div className="relative bg-[#0d0d1a] py-20 overflow-hidden">
      {/* Background texture */}
      <div
        className="absolute inset-0 opacity-5"
        style={{
          backgroundImage: 'url("data:image/svg+xml,%3Csvg width=\'60\' height=\'60\' viewBox=\'0 0 60 60\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cg fill=\'none\' fill-rule=\'evenodd\'%3E%3Cg fill=\'%23ffffff\' fill-opacity=\'1\'%3E%3Cpath d=\'M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z\'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")'
        }}
      />

      <div
        className="relative mx-auto max-w-7xl px-6 lg:px-8"
        style={{
          opacity: animationsStarted ? 1 : 0,
          transform: animationsStarted ? 'translateY(0)' : 'translateY(20px)',
          transition: 'opacity 0.8s ease-out, transform 0.8s ease-out'
        }}
      >
        {/* Section header */}
        <div className="text-center mb-16">
          <h2
            className="text-4xl sm:text-5xl font-black text-white mb-4"
            style={{ fontFamily: "'Bebas Neue', sans-serif", letterSpacing: '0.02em' }}
          >
            THE PROBLEM WITH MOCKING
          </h2>
          <p className="text-gray-400 text-lg max-w-2xl mx-auto">
            Your tests pass in a universe that doesn't exist
          </p>
        </div>

        {/* Comparison grid */}
        <div className="grid md:grid-cols-2 gap-8 lg:gap-12">
          {/* The Mock Way - Red/Warning side */}
          <div className="relative group">
            <div
              className="absolute -inset-1 bg-gradient-to-r from-red-500/20 to-orange-500/20 rounded-2xl blur-xl opacity-50 group-hover:opacity-75 transition-opacity"
            />
            <div className="relative bg-[#1a1a2e] rounded-2xl p-8 border border-red-500/20">
              {/* Header */}
              <div className="flex items-center gap-3 mb-6">
                <div className="w-12 h-12 rounded-xl bg-red-500/10 flex items-center justify-center">
                  <svg className="w-6 h-6 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-xl font-bold text-red-400" style={{ fontFamily: "'DM Sans', sans-serif" }}>
                    The Mock Way
                  </h3>
                  <p className="text-sm text-gray-500">Imagination codified</p>
                </div>
              </div>

              {/* Code block */}
              <div className="bg-[#0a0a14] rounded-xl p-4 mb-6 font-mono text-sm overflow-x-auto">
                <pre className="text-gray-300">
                  <code>{'var mockService = new Mock<IUserService>();\nmockService.Setup(x => x.GetUser(It.IsAny<int>()))\n    .Returns(new User { Id = 1, Name = "Test" });\n\n// Does this match reality?\n// What about edge cases?\n// API changed last week?\n\nvar result = await controller.GetUser(1);\nAssert.IsNotNull(result); // Passes... but means nothing'}</code>
                </pre>
              </div>

              {/* Problems list */}
              <ul className="space-y-3">
                {[
                  'Constant maintenance as APIs evolve',
                  'Edge cases easily missed',
                  'Tests pass, production fails',
                  'Mock behavior != real behavior'
                ].map((problem, i) => (
                  <li key={i} className="flex items-start gap-3 text-gray-400">
                    <svg className="w-5 h-5 text-red-400 mt-0.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                    <span>{problem}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* The Breakdance Way - Green/Success side */}
          <div className="relative group">
            <div
              className="absolute -inset-1 bg-gradient-to-r from-[#C5E842]/20 to-[#3CD0E2]/20 rounded-2xl blur-xl opacity-50 group-hover:opacity-75 transition-opacity"
            />
            <div className="relative bg-[#1a1a2e] rounded-2xl p-8 border border-[#C5E842]/20">
              {/* Header */}
              <div className="flex items-center gap-3 mb-6">
                <div className="w-12 h-12 rounded-xl bg-[#C5E842]/10 flex items-center justify-center">
                  <svg className="w-6 h-6 text-[#C5E842]" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-xl font-bold text-[#C5E842]" style={{ fontFamily: "'DM Sans', sans-serif" }}>
                    The Breakdance Way
                  </h3>
                  <p className="text-sm text-gray-500">Reality captured</p>
                </div>
              </div>

              {/* Code block */}
              <div className="bg-[#0a0a14] rounded-xl p-4 mb-6 font-mono text-sm overflow-x-auto">
                <pre className="text-gray-300">
                  <code>{'// Capture real API response once\nvar capture = new ResponseSnapshotCaptureHandler("Snapshots");\nawait client.GetAsync("https://api.example.com/users/1");\n// Real response saved to disk\n\n// Replay in all future tests - fast & deterministic\nvar replay = new ResponseSnapshotReplayHandler("Snapshots");\nvar response = await testClient.GetAsync("/users/1");\n// Exact response from real API'}</code>
                </pre>
              </div>

              {/* Benefits list */}
              <ul className="space-y-3">
                {[
                  'Test against actual API responses',
                  'Edge cases naturally captured',
                  'API changes surface immediately',
                  'Same code paths as production'
                ].map((benefit, i) => (
                  <li key={i} className="flex items-start gap-3 text-gray-400">
                    <svg className="w-5 h-5 text-[#C5E842] mt-0.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                    <span>{benefit}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
