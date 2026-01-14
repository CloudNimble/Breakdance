export const HeroSection = () => {
  return (
    <div
      className="relative overflow-hidden py-20 sm:py-28"
      style={{
        background: `
          linear-gradient(135deg, #0a0a14 0%, #0d1117 50%, #0a0a14 100%)
        `
      }}
    >
      {/* Glowing orbs background */}
      <div
        className="absolute top-0 left-1/4 w-96 h-96 rounded-full blur-3xl"
        style={{ background: 'rgba(197, 232, 66, 0.08)' }}
      />
      <div
        className="absolute bottom-0 right-1/4 w-96 h-96 rounded-full blur-3xl"
        style={{ background: 'rgba(60, 208, 226, 0.08)' }}
      />
      <div
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] rounded-full blur-3xl"
        style={{ background: 'rgba(65, 154, 197, 0.05)' }}
      />

      {/* Grid pattern overlay */}
      <div
        className="absolute inset-0 opacity-[0.03]"
        style={{
          backgroundImage: `
            linear-gradient(rgba(197, 232, 66, 0.5) 1px, transparent 1px),
            linear-gradient(90deg, rgba(197, 232, 66, 0.5) 1px, transparent 1px)
          `,
          backgroundSize: '50px 50px'
        }}
      />

      {/* Diagonal accent stripes */}
      <div className="absolute inset-0 overflow-hidden">
        <div
          className="absolute -top-10 -right-10 w-80 h-1"
          style={{
            background: 'linear-gradient(90deg, transparent, #C5E842, transparent)',
            transform: 'rotate(-35deg)',
            boxShadow: '0 0 40px 2px rgba(197, 232, 66, 0.4)'
          }}
        />
        <div
          className="absolute top-20 -right-20 w-96 h-0.5"
          style={{
            background: 'linear-gradient(90deg, transparent, #3CD0E2, transparent)',
            transform: 'rotate(-35deg)',
            boxShadow: '0 0 30px 2px rgba(60, 208, 226, 0.3)'
          }}
        />
        <div
          className="absolute top-40 -right-10 w-64 h-0.5"
          style={{
            background: 'linear-gradient(90deg, transparent, #C5E842, transparent)',
            transform: 'rotate(-35deg)',
            opacity: 0.5
          }}
        />
      </div>

      {/* City skyline at bottom - using actual SVG */}
      <svg
        className="absolute bottom-0 left-0 right-0 w-full h-24"
        viewBox="0 0 1200 100"
        preserveAspectRatio="none"
        style={{ opacity: 0.15 }}
      >
        <defs>
          <linearGradient id="skylineGradient" x1="0%" y1="0%" x2="0%" y2="100%">
            <stop offset="0%" stopColor="#3CD0E2" stopOpacity="0.5" />
            <stop offset="100%" stopColor="#1a1a2e" stopOpacity="1" />
          </linearGradient>
        </defs>
        <path
          d="M0 100 L0 70 L30 70 L30 50 L50 50 L50 70 L80 70 L80 35 L100 35 L100 70 L130 70 L130 55 L150 55 L150 25 L170 25 L170 70 L200 70 L200 60 L240 60 L240 40 L260 40 L260 70 L300 70 L300 45 L320 45 L320 20 L340 20 L340 45 L360 45 L360 70 L400 70 L400 55 L430 55 L430 30 L450 30 L450 10 L470 10 L470 30 L490 30 L490 70 L530 70 L530 50 L560 50 L560 35 L580 35 L580 70 L620 70 L620 60 L650 60 L650 40 L670 40 L670 15 L690 15 L690 40 L710 40 L710 70 L750 70 L750 55 L790 55 L790 35 L810 35 L810 70 L850 70 L850 50 L870 50 L870 25 L890 25 L890 5 L910 5 L910 25 L930 25 L930 70 L970 70 L970 60 L1010 60 L1010 45 L1030 45 L1030 70 L1070 70 L1070 55 L1090 55 L1090 30 L1110 30 L1110 70 L1150 70 L1150 50 L1170 50 L1170 70 L1200 70 L1200 100 Z"
          fill="url(#skylineGradient)"
        />
      </svg>

      {/* Main content */}
      <div className="relative mx-auto max-w-6xl px-6 lg:px-8">
        <div className="flex flex-col items-center text-center">

          {/* Logo - MUCH bigger */}
          <div className="mb-10">
            <img
              src="/breakdance-logo.png"
              alt="Breakdance"
              className="h-40 sm:h-48 w-auto"
              style={{
                filter: 'drop-shadow(0 0 40px rgba(197, 232, 66, 0.4)) drop-shadow(0 0 80px rgba(60, 208, 226, 0.2))'
              }}
            />
          </div>

          {/* Main headline - larger and bolder */}
          <h1
            className="text-6xl sm:text-8xl lg:text-9xl font-black tracking-tight mb-8"
            style={{
              fontFamily: "'Bebas Neue', 'Impact', sans-serif",
              letterSpacing: '0.03em',
              textShadow: '0 0 80px rgba(197, 232, 66, 0.3)'
            }}
          >
            <span style={{ color: '#ffffff' }}>TEST </span>
            <span
              style={{
                color: '#C5E842',
                textShadow: '0 0 60px rgba(197, 232, 66, 0.6), 0 0 120px rgba(197, 232, 66, 0.3)'
              }}
            >
              REAL
            </span>
            <span style={{ color: '#ffffff' }}> THINGS</span>
          </h1>

          {/* Tagline with proper spacing */}
          <div
            className="flex flex-wrap items-center justify-center gap-3 sm:gap-4 mb-10"
            style={{ fontFamily: "'JetBrains Mono', 'Consolas', monospace" }}
          >
            <span
              className="text-xl sm:text-2xl lg:text-3xl font-bold px-4 py-2 rounded-lg"
              style={{
                color: '#C5E842',
                background: 'rgba(197, 232, 66, 0.1)',
                border: '1px solid rgba(197, 232, 66, 0.2)'
              }}
            >
              No Fakes.
            </span>
            <span className="text-2xl sm:text-3xl text-gray-600 font-light">//</span>
            <span
              className="text-xl sm:text-2xl lg:text-3xl font-bold px-4 py-2 rounded-lg"
              style={{
                color: '#3CD0E2',
                background: 'rgba(60, 208, 226, 0.1)',
                border: '1px solid rgba(60, 208, 226, 0.2)'
              }}
            >
              No Mocks.
            </span>
            <span className="text-2xl sm:text-3xl text-gray-600 font-light">//</span>
            <span
              className="text-xl sm:text-2xl lg:text-3xl font-bold px-4 py-2 rounded-lg"
              style={{
                color: '#ffffff',
                background: 'rgba(255, 255, 255, 0.05)',
                border: '1px solid rgba(255, 255, 255, 0.1)'
              }}
            >
              Ever.
            </span>
          </div>

          {/* Description */}
          <p
            className="text-lg sm:text-xl text-gray-400 max-w-3xl mb-12 leading-relaxed"
            style={{ fontFamily: "'DM Sans', system-ui, sans-serif" }}
          >
            A .NET testing framework that captures real behavior instead of imagining it.
            Test against actual HTTP responses, real infrastructure, and the same code paths production uses.
          </p>

          {/* CTA Buttons - bigger and bolder */}
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 sm:gap-6">
            <a
              href="/guides/index"
              className="group relative px-10 py-5 text-xl font-bold rounded-xl overflow-hidden transition-all duration-300 hover:scale-105"
              style={{
                fontFamily: "'DM Sans', sans-serif",
                background: '#C5E842',
                color: '#0a0a14',
                boxShadow: '0 0 40px rgba(197, 232, 66, 0.4)'
              }}
            >
              <span className="relative z-10">Get Started</span>
              <div
                className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-300"
                style={{ background: 'linear-gradient(135deg, #C5E842, #3CD0E2)' }}
              />
            </a>
            <a
              href="/why-breakdance"
              className="px-10 py-5 text-xl font-bold rounded-xl transition-all duration-300 hover:scale-105"
              style={{
                fontFamily: "'DM Sans', sans-serif",
                color: '#3CD0E2',
                border: '2px solid #3CD0E2',
                background: 'rgba(60, 208, 226, 0.05)'
              }}
            >
              Why Breakdance? →
            </a>
          </div>

        </div>
      </div>

      {/* Bottom gradient fade */}
      <div
        className="absolute bottom-0 left-0 right-0 h-32 pointer-events-none"
        style={{
          background: 'linear-gradient(to top, #0d0d1a 0%, transparent 100%)'
        }}
      />
    </div>
  );
};
