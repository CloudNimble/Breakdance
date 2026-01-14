export const QuickStart = () => {
  const steps = [
    {
      number: '01',
      title: 'Install',
      description: 'Add the core package to your test project',
      code: 'dotnet add package Breakdance.Assemblies'
    },
    {
      number: '02',
      title: 'Capture',
      description: 'Record real responses from actual APIs',
      code: `var handler = new ResponseSnapshotCaptureHandler("Snapshots") {
    InnerHandler = new HttpClientHandler()
};
var client = new HttpClient(handler);
await client.GetAsync("https://api.example.com/users");
// Response saved to Snapshots/api.example.com/users.json`
    },
    {
      number: '03',
      title: 'Replay',
      description: 'Use captured responses in your tests',
      code: `[TestMethod]
public async Task GetUsers_ReturnsExpectedData()
{
    var handler = new ResponseSnapshotReplayHandler("Snapshots");
    var client = new HttpClient(handler);

    var response = await client.GetAsync("https://api.example.com/users");

    response.IsSuccessStatusCode.Should().BeTrue();
}`
    },
    {
      number: '04',
      title: 'Commit',
      description: 'Check snapshots into source control',
      code: `git add Snapshots/
git commit -m "Add API response snapshots"
# Now your entire team can run tests without API access`
    }
  ];

  return (
    <div className="relative bg-[#0a0a14] py-20">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        {/* Section header */}
        <div className="text-center mb-16">
          <h2
            className="text-4xl sm:text-5xl font-black text-white mb-4"
            style={{ fontFamily: "'Bebas Neue', sans-serif", letterSpacing: '0.02em' }}
          >
            START IN MINUTES
          </h2>
          <p className="text-gray-400 text-lg max-w-2xl mx-auto">
            Four steps to testing real things
          </p>
        </div>

        {/* Steps */}
        <div className="space-y-8">
          {steps.map((step, index) => (
            <div
              key={index}
              className="relative flex flex-col lg:flex-row gap-6 items-start"
            >
              {/* Step number */}
              <div className="flex-shrink-0 flex items-center gap-4 lg:w-48">
                <span
                  className="text-5xl font-black"
                  style={{
                    fontFamily: "'Bebas Neue', sans-serif",
                    background: 'linear-gradient(135deg, #C5E842, #3CD0E2)',
                    WebkitBackgroundClip: 'text',
                    WebkitTextFillColor: 'transparent'
                  }}
                >
                  {step.number}
                </span>
                <div>
                  <h3 className="text-xl font-bold text-white" style={{ fontFamily: "'DM Sans', sans-serif" }}>
                    {step.title}
                  </h3>
                  <p className="text-sm text-gray-500">{step.description}</p>
                </div>
              </div>

              {/* Connector line */}
              {index < steps.length - 1 && (
                <div
                  className="hidden lg:block absolute left-[1.75rem] top-16 w-0.5 h-8"
                  style={{
                    background: 'linear-gradient(to bottom, #C5E842, transparent)'
                  }}
                />
              )}

              {/* Code block */}
              <div className="flex-1 w-full">
                <div className="bg-[#1a1a2e] rounded-xl border border-gray-800 overflow-hidden">
                  <div className="flex items-center gap-2 px-4 py-2 border-b border-gray-800">
                    <div className="w-3 h-3 rounded-full bg-red-500/60" />
                    <div className="w-3 h-3 rounded-full bg-yellow-500/60" />
                    <div className="w-3 h-3 rounded-full bg-green-500/60" />
                  </div>
                  <pre className="p-4 overflow-x-auto">
                    <code className="text-sm font-mono text-gray-300">{step.code}</code>
                  </pre>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
