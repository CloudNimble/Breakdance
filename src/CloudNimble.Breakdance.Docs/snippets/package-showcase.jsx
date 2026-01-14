export const PackageShowcase = () => {
  const packages = [
    {
      name: 'Breakdance.Assemblies',
      description: 'Core testing base classes, DI management, response snapshots, public API analysis',
      icon: '🎯',
      primary: true
    },
    {
      name: 'Breakdance.AspNetCore',
      description: 'In-memory TestServer for ASP.NET Core with full DI support',
      icon: '⚡'
    },
    {
      name: 'Breakdance.WebApi',
      description: 'In-memory testing for ASP.NET Web API 2 on .NET Framework',
      icon: '🏛️'
    },
    {
      name: 'Breakdance.DotHttp',
      description: '.http file parsing, variables, chaining, and environment configs',
      icon: '📄'
    },
    {
      name: 'Breakdance.Azurite',
      description: 'Azure Storage testing with Blob, Queue, and Table support',
      icon: '☁️'
    },
    {
      name: 'Breakdance.Blazor',
      description: 'Blazor component testing with bUnit integration',
      icon: '🔥'
    }
  ];

  return (
    <div className="relative bg-[#0d0d1a] py-20 overflow-hidden">
      {/* Static background lines */}
      <div className="absolute inset-0 opacity-10">
        {[...Array(5)].map((_, i) => (
          <div
            key={i}
            className="absolute h-px bg-gradient-to-r from-transparent via-[#3CD0E2] to-transparent"
            style={{
              top: `${20 + i * 20}%`,
              left: '0',
              right: '0'
            }}
          />
        ))}
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        {/* Section header */}
        <div className="text-center mb-16">
          <h2
            className="text-4xl sm:text-5xl font-black text-white mb-4"
            style={{ fontFamily: "'Bebas Neue', sans-serif", letterSpacing: '0.02em' }}
          >
            THE BREAKDANCE FAMILY
          </h2>
          <p className="text-gray-400 text-lg max-w-2xl mx-auto">
            Install only what you need. Each package is focused and lightweight.
          </p>
        </div>

        {/* Install command */}
        <div className="max-w-2xl mx-auto mb-12">
          <div className="bg-[#1a1a2e] rounded-xl p-4 border border-gray-800">
            <div className="flex items-center gap-2 mb-2">
              <div className="w-3 h-3 rounded-full bg-red-500/80" />
              <div className="w-3 h-3 rounded-full bg-yellow-500/80" />
              <div className="w-3 h-3 rounded-full bg-green-500/80" />
              <span className="ml-2 text-xs text-gray-500 font-mono">terminal</span>
            </div>
            <code className="text-[#C5E842] font-mono text-sm sm:text-base">
              dotnet add package Breakdance.Assemblies
            </code>
          </div>
        </div>

        {/* Package grid */}
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {packages.map((pkg, index) => (
            <div
              key={index}
              className={`
                relative group rounded-xl p-5 border transition-all duration-300 hover:-translate-y-1
                ${pkg.primary
                  ? 'bg-gradient-to-br from-[#C5E842]/10 to-[#3CD0E2]/10 border-[#C5E842]/30'
                  : 'bg-[#1a1a2e]/50 border-gray-800 hover:border-gray-700'
                }
              `}
            >
              {pkg.primary && (
                <div className="absolute -top-2 -right-2 px-2 py-0.5 bg-[#C5E842] text-[#0a0a14] text-xs font-bold rounded-full">
                  CORE
                </div>
              )}

              <div className="flex items-start gap-4">
                <span className="text-2xl">{pkg.icon}</span>
                <div className="flex-1 min-w-0">
                  <h3
                    className="font-mono text-sm font-bold text-white mb-1 truncate"
                    style={{ color: pkg.primary ? '#C5E842' : 'white' }}
                  >
                    {pkg.name}
                  </h3>
                  <p className="text-xs text-gray-400 leading-relaxed">
                    {pkg.description}
                  </p>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Platform support */}
        <div className="mt-12 text-center">
          <p className="text-sm text-gray-500 mb-4">Supports</p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            {['.NET 4.8', '.NET Standard 2.0', '.NET 6', '.NET 8', '.NET 9', '.NET 10'].map((platform, i) => (
              <span
                key={i}
                className="px-3 py-1 bg-[#1a1a2e] border border-gray-800 rounded-full text-xs font-mono text-gray-400"
              >
                {platform}
              </span>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
