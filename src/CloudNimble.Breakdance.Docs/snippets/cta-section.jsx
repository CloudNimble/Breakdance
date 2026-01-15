export const CTASection = () => {
  const [animationsStarted, setAnimationsStarted] = React.useState(false);

  React.useEffect(() => {
    setAnimationsStarted(true);
  }, []);

  return (
    <div className="relative bg-[#0d0d1a] py-24 overflow-hidden">
      {/* Animated gradient background */}
      <div
        className="absolute inset-0 opacity-30"
        style={{
          background: 'radial-gradient(ellipse at 30% 50%, rgba(197, 232, 66, 0.2) 0%, transparent 50%), radial-gradient(ellipse at 70% 50%, rgba(60, 208, 226, 0.2) 0%, transparent 50%)'
        }}
      />

      {/* City skyline bottom */}
      <div
        className="absolute bottom-0 left-0 right-0 h-24 opacity-10"
        style={{
          background: '#1a1a2e',
          maskImage: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1200 80' preserveAspectRatio='none'%3E%3Cpath d='M0 80 L0 60 L30 60 L30 40 L50 40 L50 60 L80 60 L80 30 L100 30 L100 60 L140 60 L140 45 L160 45 L160 20 L180 20 L180 60 L220 60 L220 50 L260 50 L260 35 L280 35 L280 60 L320 60 L320 40 L340 40 L340 15 L360 15 L360 40 L380 40 L380 60 L420 60 L420 55 L460 55 L460 25 L480 25 L480 60 L520 60 L520 45 L540 45 L540 60 L580 60 L580 35 L600 35 L600 10 L620 10 L620 35 L640 35 L640 60 L680 60 L680 50 L720 50 L720 30 L740 30 L740 60 L780 60 L780 40 L800 40 L800 20 L820 20 L820 60 L860 60 L860 55 L900 55 L900 35 L920 35 L920 60 L960 60 L960 45 L980 45 L980 25 L1000 25 L1000 60 L1040 60 L1040 50 L1080 50 L1080 60 L1120 60 L1120 40 L1140 40 L1140 60 L1200 60 L1200 80 Z' fill='white'/%3E%3C/svg%3E\")",
          maskSize: '100% 100%',
          WebkitMaskImage: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1200 80' preserveAspectRatio='none'%3E%3Cpath d='M0 80 L0 60 L30 60 L30 40 L50 40 L50 60 L80 60 L80 30 L100 30 L100 60 L140 60 L140 45 L160 45 L160 20 L180 20 L180 60 L220 60 L220 50 L260 50 L260 35 L280 35 L280 60 L320 60 L320 40 L340 40 L340 15 L360 15 L360 40 L380 40 L380 60 L420 60 L420 55 L460 55 L460 25 L480 25 L480 60 L520 60 L520 45 L540 45 L540 60 L580 60 L580 35 L600 35 L600 10 L620 10 L620 35 L640 35 L640 60 L680 60 L680 50 L720 50 L720 30 L740 30 L740 60 L780 60 L780 40 L800 40 L800 20 L820 20 L820 60 L860 60 L860 55 L900 55 L900 35 L920 35 L920 60 L960 60 L960 45 L980 45 L980 25 L1000 25 L1000 60 L1040 60 L1040 50 L1080 50 L1080 60 L1120 60 L1120 40 L1140 40 L1140 60 L1200 60 L1200 80 Z' fill='white'/%3E%3C/svg%3E\")",
          WebkitMaskSize: '100% 100%'
        }}
      />

      <div
        className="relative mx-auto max-w-4xl px-6 lg:px-8 text-center"
        style={{
          opacity: animationsStarted ? 1 : 0,
          transform: animationsStarted ? 'translateY(0)' : 'translateY(20px)',
          transition: 'opacity 0.8s ease-out, transform 0.8s ease-out'
        }}
      >
        {/* Headline */}
        <h2
          className="text-4xl sm:text-6xl font-black text-white mb-6"
          style={{ fontFamily: "'Bebas Neue', sans-serif", letterSpacing: '0.02em' }}
        >
          READY TO{' '}
          <span
            style={{
              background: 'linear-gradient(135deg, #C5E842, #3CD0E2)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent'
            }}
          >
            TEST REAL THINGS
          </span>
          ?
        </h2>

        {/* Subtext */}
        <p className="text-xl text-gray-400 mb-10 max-w-2xl mx-auto leading-relaxed">
          Join developers who have eliminated mocking from their test suites.
          Your tests will be faster, more reliable, and actually mean something.
        </p>

        {/* CTA buttons */}
        <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
          <a
            href="/guides/index"
            className="group relative px-10 py-5 bg-[#C5E842] text-[#0a0a14] font-bold text-lg rounded-xl overflow-hidden transition-all duration-300 hover:scale-105 shadow-[0_0_40px_rgba(197,232,66,0.3)]"
            style={{ fontFamily: "'DM Sans', sans-serif" }}
          >
            <span className="relative z-10 flex items-center gap-2">
              Explore the Guides
              <svg className="w-5 h-5 transition-transform group-hover:translate-x-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
              </svg>
            </span>
          </a>
          <a
            href="/api-reference/CloudNimble/Breakdance/Assemblies/index"
            className="px-10 py-5 border-2 border-[#3CD0E2] text-[#3CD0E2] font-bold text-lg rounded-xl transition-all duration-300 hover:bg-[#3CD0E2] hover:text-[#0a0a14]"
            style={{ fontFamily: "'DM Sans', sans-serif" }}
          >
            API Reference
          </a>
        </div>

        {/* Stats */}
        <div className="mt-16 grid grid-cols-3 gap-8 max-w-lg mx-auto">
          {[
            { value: '6', label: 'Packages' },
            { value: '10+', label: '.NET Versions' },
            { value: '0', label: 'Mocks Required' }
          ].map((stat, i) => (
            <div key={i} className="text-center">
              <div
                className="text-3xl font-black mb-1"
                style={{
                  fontFamily: "'Bebas Neue', sans-serif",
                  color: i === 2 ? '#C5E842' : 'white'
                }}
              >
                {stat.value}
              </div>
              <div className="text-xs text-gray-500 uppercase tracking-wider">
                {stat.label}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
