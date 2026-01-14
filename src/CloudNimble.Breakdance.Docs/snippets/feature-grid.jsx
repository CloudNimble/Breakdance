export const FeatureGrid = () => {
  const features = [
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
      ),
      title: 'Response Snapshots',
      description: 'Capture real HTTP responses and replay them instantly. No network, no rate limits, works offline.',
      href: '/guides/web/snapshots/responses',
      color: '#C5E842'
    },
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
      ),
      title: 'Request Snapshots',
      description: 'Define API requests in Visual Studio\'s .http format. Variables, chaining, and environment configs built-in.',
      href: '/guides/web/snapshots/requests',
      color: '#3CD0E2'
    },
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M5 12h14M5 12a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v4a2 2 0 01-2 2M5 12a2 2 0 00-2 2v4a2 2 0 002 2h14a2 2 0 002-2v-4a2 2 0 00-2-2" />
        </svg>
      ),
      title: 'In-Memory TestServer',
      description: 'Run your actual ASP.NET pipeline in-memory. Same DI container, same middleware, same behavior.',
      href: '/guides/web/aspnet-core-rest',
      color: '#C5E842'
    },
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
        </svg>
      ),
      title: 'DI Container Testing',
      description: 'Full IHost management with GetService<T>, scoped services, keyed services (.NET 8+), and container diagnostics.',
      href: '/api-reference/CloudNimble/Breakdance/Assemblies/BreakdanceTestBase',
      color: '#3CD0E2'
    },
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
        </svg>
      ),
      title: 'Public API Analysis',
      description: 'Generate API surface reports, detect breaking changes before release with TypeDefinition mappings.',
      href: '/api-reference/CloudNimble/Breakdance/Assemblies/PublicApiHelpers',
      color: '#C5E842'
    },
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
        </svg>
      ),
      title: 'Identity & Claims',
      description: 'Test authorization without complex identity setup. SetThreadPrincipal and claims management built-in.',
      href: '/api-reference/CloudNimble/Breakdance/Assemblies/BreakdanceTestBase',
      color: '#3CD0E2'
    },
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
        </svg>
      ),
      title: 'Private Member Access',
      description: 'PrivateObject and PrivateType classes let you test implementation details when needed.',
      href: '/api-reference/CloudNimble/Breakdance/Assemblies/PrivateObject',
      color: '#C5E842'
    },
    {
      icon: (
        <svg className="w-7 h-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z" />
        </svg>
      ),
      title: 'Azure Storage Testing',
      description: 'Test against real Azurite - actual Azure Storage API, not mocks. Blob, Queue, and Table support.',
      href: '/guides/testing-azure-storage',
      color: '#3CD0E2'
    }
  ];

  return (
    <div className="relative bg-[#0a0a14] py-20">
      {/* Gradient accent */}
      <div
        className="absolute top-0 left-1/2 -translate-x-1/2 w-3/4 h-px"
        style={{
          background: 'linear-gradient(90deg, transparent, #C5E842, #3CD0E2, transparent)'
        }}
      />

      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        {/* Section header */}
        <div className="text-center mb-16">
          <h2
            className="text-4xl sm:text-5xl font-black text-white mb-4"
            style={{ fontFamily: "'Bebas Neue', sans-serif", letterSpacing: '0.02em' }}
          >
            EVERYTHING YOU NEED
          </h2>
          <p className="text-gray-400 text-lg max-w-2xl mx-auto">
            A complete toolkit for testing real behavior across your .NET applications
          </p>
        </div>

        {/* Feature grid */}
        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {features.map((feature, index) => (
            <a
              key={index}
              href={feature.href}
              className="group relative bg-[#1a1a2e]/50 rounded-2xl p-6 border border-gray-800 hover:border-gray-700 transition-all duration-300 hover:-translate-y-1"
            >
              {/* Hover glow */}
              <div
                className="absolute inset-0 rounded-2xl opacity-0 group-hover:opacity-100 transition-opacity duration-300 -z-10 blur-xl"
                style={{ background: `${feature.color}10` }}
              />

              {/* Icon */}
              <div
                className="w-14 h-14 rounded-xl flex items-center justify-center mb-4 transition-transform duration-300 group-hover:scale-110"
                style={{
                  background: `${feature.color}15`,
                  color: feature.color
                }}
              >
                {feature.icon}
              </div>

              {/* Content */}
              <h3
                className="text-lg font-bold text-white mb-2 group-hover:text-[#C5E842] transition-colors"
                style={{ fontFamily: "'DM Sans', sans-serif" }}
              >
                {feature.title}
              </h3>
              <p className="text-sm text-gray-400 leading-relaxed">
                {feature.description}
              </p>

              {/* Arrow indicator */}
              <div className="mt-4 flex items-center text-sm font-medium" style={{ color: feature.color }}>
                <span className="opacity-0 -translate-x-2 group-hover:opacity-100 group-hover:translate-x-0 transition-all duration-300">
                  Learn more →
                </span>
              </div>
            </a>
          ))}
        </div>
      </div>
    </div>
  );
};
