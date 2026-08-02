"use client";

import { useRef, useState } from "react";
import { motion, useInView, useScroll, useTransform } from "framer-motion";
import {
  Leaf,
  Shield,
  CloudRain,
  Satellite,
  Brain,
  Wallet,
  Clock,
  MapPin,
  Sprout,
  TrendingUp,
  CheckCircle2,
  ArrowRight,
  Star,
  Quote,
  ChevronDown,
  Award,
  Camera,
} from "lucide-react";
import { useApp } from "@/lib/store";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { cn, formatINR } from "@/lib/utils";
import { Navbar } from "@/components/layout/Navbar";

// Curated real Unsplash farm/agriculture photos
const HERO_IMG =
  "https://images.unsplash.com/photo-1592982537447-7440770cbfc9?auto=format&fit=crop&w=2000&q=80";
const HERO_IMG_2 =
  "https://images.unsplash.com/photo-1500382017468-9049fed747ef?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80";
const FARMER_PORTRAIT =
  "https://images.unsplash.com/photo-1607990281513-2c110a25bd8c?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80";
const FIELD_TOP =
  "https://images.unsplash.com/photo-1500382017468-9049fed747ef?ixlib=rb-4.0.3&auto=format&fit=crop&w=1600&q=80";

const features = [
  {
    icon: Brain,
    title: "AI-Powered Claim Assessment",
    desc: "Satellite imagery + drone photos + weather data, fused by ML to verify damage in hours, not weeks. Every claim gets an unbiased, transparent AI damage score.",
    accent: "from-emerald-500/20 to-green-600/10",
    iconBg: "bg-emerald-600",
  },
  {
    icon: CloudRain,
    title: "Real-Time Weather Verification",
    desc: "We pull live weather snapshots from IMD at the exact moment of incident — rainfall, hail, frost — so your claim is backed by hard meteorological evidence.",
    accent: "from-blue-500/20 to-cyan-600/10",
    iconBg: "bg-blue-600",
  },
  {
    icon: Wallet,
    title: "Instant Payouts",
    desc: "Approved claims are disbursed directly to your bank via Razorpay within 24 hours. No paperwork, no agent visits, no waiting rooms.",
    accent: "from-amber-500/20 to-orange-600/10",
    iconBg: "bg-amber-600",
  },
  {
    icon: MapPin,
    title: "Geo-Tagged Farms",
    desc: "Register your farm's exact boundary with GeoJSON. We monitor your plots remotely and proactively alert you to weather risks before they escalate.",
    accent: "from-rose-500/20 to-pink-600/10",
    iconBg: "bg-rose-600",
  },
  {
    icon: Satellite,
    title: "Satellite Damage Detection",
    desc: "NDVI change detection over your farm before and after an incident gives our reviewers objective evidence — no more disputes about extent of damage.",
    accent: "from-indigo-500/20 to-purple-600/10",
    iconBg: "bg-indigo-600",
  },
  {
    icon: Shield,
    title: "Bank-Grade Security",
    desc: "Your land records and policies are encrypted at rest and in transit. Role-based access, audit logs, and fraud detection on every transaction.",
    accent: "from-teal-500/20 to-emerald-600/10",
    iconBg: "bg-teal-600",
  },
];

const steps = [
  {
    n: "01",
    title: "Register your farm",
    desc: "Sign up, geo-tag your plot, and add a few details about what you grow. Takes 4 minutes.",
    icon: MapPin,
  },
  {
    n: "02",
    title: "Pick an insurance plan",
    desc: "Browse AI-curated plans for your crop and region. Compare premium, coverage and payout terms.",
    icon: Sprout,
  },
  {
    n: "03",
    title: "Pay premium online",
    desc: "Secure UPI/card payment via Razorpay. Your policy is issued instantly and stored digitally.",
    icon: Wallet,
  },
  {
    n: "04",
    title: "File a claim with photos",
    desc: "When disaster strikes, snap a few photos, describe the damage. Our AI cross-checks weather + satellite.",
    icon: Camera,
  },
  {
    n: "05",
    title: "Get paid in 24 hours",
    desc: "Approved amount lands in your bank the next day. No agents, no forms, no waiting.",
    icon: TrendingUp,
  },
];

const stats = [
  { value: "24 hrs", label: "Claim payout target", icon: Clock },
  { value: "AI", label: "Satellite-verified claims", icon: Brain },
  { value: "100%", label: "Paperless process", icon: CheckCircle2 },
  { value: "IMD", label: "Live weather data", icon: CloudRain },
];

const plans = [
  {
    name: "Kharif Paddy Shield",
    crop: "Paddy",
    provider: "FarmGuard Insurance",
    premium: 1850,
    coverage: 45000,
    coveragePct: 90,
    duration: "6 months",
    color: "from-emerald-600 to-green-700",
    popular: true,
  },
  {
    name: "Wheat Harvest Protect",
    crop: "Wheat",
    provider: "AgriSure Mutual",
    premium: 1450,
    coverage: 38000,
    coveragePct: 85,
    duration: "5 months",
    color: "from-amber-600 to-orange-700",
    popular: false,
  },
  {
    name: "Cotton Comprehensive",
    crop: "Cotton",
    provider: "Bharat Agri Insurance",
    premium: 2200,
    coverage: 52000,
    coveragePct: 88,
    duration: "12 months",
    color: "from-rose-600 to-red-700",
    popular: false,
  },
];

const testimonials = [
  {
    name: "Amara Singh",
    location: "Krishna District, Andhra Pradesh",
    quote:
      "When floods hit my paddy in August, I filed a claim from my phone at 8pm. By 4pm next day, ₹1.97 lakh was in my account. I didn't believe it was real.",
    crop: "Paddy Farmer",
    img: "https://images.unsplash.com/photo-1595152772835-219674b2a8a6?ixlib=rb-4.0.3&auto=format&fit=crop&w=200&q=80",
  },
  {
    name: "Lakshmi Reddy",
    location: "Godavari Belt, Andhra Pradesh",
    quote:
      "The satellite imagery caught pest damage I hadn't even noticed yet. FarmClaim proactively reached out and walked me through filing a claim. Felt like the future.",
    crop: "Cotton Farmer",
    img: "https://images.unsplash.com/photo-1594744803329-e58b31de8bf5?ixlib=rb-4.0.3&auto=format&fit=crop&w=200&q=80",
  },
  {
    name: "Bharat Yadav",
    location: "Hisar, Haryana",
    quote:
      "Last year's hailstorm destroyed my wheat. The old insurer took 3 months to send an agent who offered me ₹4,000. With FarmClaim, the AI approved ₹38,500 in 19 hours.",
    crop: "Wheat Farmer",
    img: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?ixlib=rb-4.0.3&auto=format&fit=crop&w=200&q=80",
  },
];

const faqs = [
  {
    q: "How does the AI actually assess damage?",
    a: "We combine three signals: (1) satellite NDVI change detection over your geo-tagged plot, before vs after the incident date; (2) weather records from IMD at the exact time and location; (3) the photos you upload, analysed by a vision model trained on 2.4M crop damage images. The output is a damage percentage with a confidence score, fully transparent in your claim dashboard.",
  },
  {
    q: "What if I disagree with the AI's assessment?",
    a: "Every AI result is reviewed by a human claims adjuster before payout. You can also submit additional photos or request a drone inspection through your dashboard. Disputes are resolved within 7 working days.",
  },
  {
    q: "Which crops and states are covered?",
    a: "We currently offer plans for paddy, wheat, cotton, sugarcane, pulses and horticulture across Andhra Pradesh, Telangana, Maharashtra, Karnataka, Punjab and Haryana. New crop types and states are added every quarter.",
  },
  {
    q: "How fast is the payout really?",
    a: "If your claim is approved (most straightforward weather claims are), the amount is disbursed via Razorpay to your linked bank account within 24 hours. Fast, direct, and trackable in your dashboard.",
  },
  {
    q: "Is my land data safe?",
    a: "Yes. All land records, GeoJSON boundaries and personal information are AES-256 encrypted at rest and TLS 1.3 in transit. We never sell data. Access is logged in an immutable audit trail. You can export or delete your data at any time.",
  },
];

export function LandingPage() {
  const navigate = useApp((s) => s.navigate);
  const heroRef = useRef<HTMLDivElement>(null);
  const { scrollYProgress } = useScroll({
    target: heroRef,
    offset: ["start start", "end start"],
  });
  const heroY = useTransform(scrollYProgress, [0, 1], [0, 200]);
  const heroScale = useTransform(scrollYProgress, [0, 1], [1, 1.15]);
  const heroOpacity = useTransform(scrollYProgress, [0, 0.7], [1, 0]);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      {/* HERO */}
      <section
        ref={heroRef}
        className="relative min-h-[100svh] flex items-center pt-28 pb-20 overflow-hidden"
      >
        {/* Background image with parallax */}
        <motion.div
          style={{ y: heroY, scale: heroScale }}
          className="absolute inset-0 z-0"
        >
          <img
            src={HERO_IMG}
            alt="Green paddy field at golden hour"
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-gradient-to-b from-emerald-950/40 via-emerald-950/30 to-background" />
          <div className="absolute inset-0 bg-gradient-to-r from-emerald-950/50 via-transparent to-transparent" />
        </motion.div>

        <motion.div
          style={{ opacity: heroOpacity }}
          className="container mx-auto px-4 relative z-10"
        >
          <div className="max-w-3xl">
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6 }}
            >
              <Badge
                variant="outline"
                className="mb-6 bg-white/10 backdrop-blur-md border-white/20 text-white gap-2 px-3 py-1.5"
              >
                <span className="h-2 w-2 rounded-full bg-emerald-400 pulse-ring" />
                Trusted by farmers across 6 states
              </Badge>
            </motion.div>

            <motion.h1
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.7, delay: 0.1 }}
              className="font-serif text-5xl sm:text-6xl lg:text-7xl font-semibold text-white leading-[1.05] tracking-tight text-balance"
            >
              When the weather turns,
              <br />
              <span className="bg-gradient-to-r from-amber-200 via-emerald-200 to-amber-100 bg-clip-text text-transparent">
                your harvest shouldn&apos;t burn.
              </span>
            </motion.h1>

            <motion.p
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.7, delay: 0.25 }}
              className="mt-6 text-lg sm:text-xl text-emerald-50/90 max-w-2xl leading-relaxed"
            >
              AI-powered crop insurance that pays you in hours, not months. File a
              claim with your phone, let our satellites and weather data verify
              the damage, and get money in your bank — fast.
            </motion.p>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.7, delay: 0.4 }}
              className="mt-8 flex flex-col sm:flex-row gap-3"
            >
              <Button
                size="lg"
                onClick={() => navigate("/signup")}
                className="bg-emerald-600 hover:bg-emerald-500 text-white text-base h-12 px-7 shadow-xl shadow-emerald-900/40 gap-2"
              >
                Protect your farm
                <ArrowRight className="h-4 w-4" />
              </Button>
              <Button
                size="lg"
                variant="outline"
                onClick={() => navigate("/login")}
                className="bg-white/10 backdrop-blur-md text-white border-white/30 hover:bg-white/20 hover:text-white text-base h-12 px-7"
              >
                Sign in
              </Button>
            </motion.div>

            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.7, delay: 0.6 }}
              className="mt-10 flex items-center gap-6 text-white/80 text-sm"
            >
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-emerald-300" />
                No paperwork
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-emerald-300" />
                No agent visits
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-emerald-300" />
                Cancel anytime
              </div>
            </motion.div>
          </div>
        </motion.div>

        {/* Scroll indicator */}
        <div className="absolute bottom-8 left-1/2 -translate-x-1/2 z-10">
          <motion.div
            animate={{ y: [0, 8, 0] }}
            transition={{ duration: 2, repeat: Infinity }}
            className="text-white/60"
          >
            <ChevronDown className="h-6 w-6" />
          </motion.div>
        </div>
      </section>

      {/* STATS BAR */}
      <section className="relative -mt-8 z-20">
        <div className="container mx-auto px-4">
          <Card className="border-0 shadow-2xl shadow-emerald-900/10 bg-card/95 backdrop-blur">
            <CardContent className="grid grid-cols-2 lg:grid-cols-4 gap-6 p-8">
              {stats.map((s, i) => (
                <StatItem key={i} {...s} />
              ))}
            </CardContent>
          </Card>
        </div>
      </section>

      {/* FEATURES */}
      <section id="features" className="py-24 lg:py-32">
        <div className="container mx-auto px-4">
          <SectionHeading
            eyebrow="Why FarmClaim"
            title={
              <>
                Insurance that thinks
                <br />
                <span className="gradient-text">like a farmer.</span>
              </>
            }
            subtitle="We rebuilt crop insurance from the ground up — combining satellite data, AI vision, and live weather to give every farmer a fair, fast, transparent claim experience."
          />

          <div className="mt-16 grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {features.map((f, i) => (
              <FeatureCard key={i} {...f} index={i} />
            ))}
          </div>
        </div>
      </section>

      {/* HOW IT WORKS */}
      <section id="how" className="py-24 lg:py-32 bg-emerald-950 text-white relative overflow-hidden">
        <div className="absolute inset-0 leaf-pattern opacity-10" />
        <div className="absolute top-0 right-0 w-96 h-96 bg-emerald-500/20 rounded-full blur-3xl" />
        <div className="absolute bottom-0 left-0 w-96 h-96 bg-amber-500/10 rounded-full blur-3xl" />

        <div className="container mx-auto px-4 relative">
          <div className="text-center max-w-2xl mx-auto mb-16">
            <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-emerald-500/20 text-emerald-200 text-xs font-medium uppercase tracking-widest mb-4">
              <Sprout className="h-3.5 w-3.5" />
              From seed to safety net
            </div>
            <h2 className="font-serif text-4xl lg:text-5xl font-semibold leading-tight text-balance">
              Five steps. <span className="text-emerald-300">Twenty-four hours.</span>
            </h2>
            <p className="mt-4 text-emerald-100/70 text-lg">
              From signing up to getting paid — here&apos;s the entire FarmClaim
              journey.
            </p>
          </div>

          <div className="relative">
            <div className="absolute left-1/2 top-0 bottom-0 w-px bg-gradient-to-b from-transparent via-emerald-500/30 to-transparent hidden lg:block" />
            <div className="space-y-8 lg:space-y-0">
              {steps.map((step, i) => (
                <StepRow key={i} step={step} index={i} />
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* PLANS PREVIEW */}
      <section id="plans" className="py-24 lg:py-32">
        <div className="container mx-auto px-4">
          <SectionHeading
            eyebrow="Insurance Plans"
            title={
              <>
                Coverage for every crop,
                <br />
                <span className="gradient-text">every season.</span>
              </>
            }
            subtitle="Transparent premiums, instant issuance, no hidden clauses. Browse our most popular plans below — sign up to see all available options."
          />

          <div className="mt-16 grid lg:grid-cols-3 gap-6">
            {plans.map((p, i) => (
              <PlanCard key={i} plan={p} onChoose={() => navigate("/signup")} />
            ))}
          </div>

          <div className="text-center mt-12">
            <Button
              variant="outline"
              size="lg"
              onClick={() => navigate("/signup")}
              className="border-emerald-700 text-emerald-700 hover:bg-emerald-50 gap-2"
            >
              View all plans
              <ArrowRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </section>

      {/* AI SECTION */}
      <section className="py-24 lg:py-32 bg-gradient-to-br from-emerald-50 via-amber-50/30 to-emerald-50">
        <div className="container mx-auto px-4">
          <div className="grid lg:grid-cols-2 gap-12 items-center">
            <div className="relative">
              <div className="relative rounded-3xl overflow-hidden shadow-2xl shadow-emerald-900/20">
                <img
                  src={FIELD_TOP}
                  alt="Aerial view of farm fields with NDVI analysis overlay"
                  className="w-full h-[500px] object-cover"
                />
                <div className="absolute inset-0 bg-gradient-to-tr from-emerald-950/40 to-transparent" />
              </div>
              {/* Floating cards */}
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.6, delay: 0.2 }}
                className="absolute -top-6 -right-6 bg-white rounded-2xl shadow-xl p-4 max-w-[220px] border border-emerald-100"
              >
                <div className="flex items-center gap-2 text-emerald-700 mb-2">
                  <Satellite className="h-4 w-4" />
                  <span className="text-xs font-semibold uppercase tracking-wide">
                    Satellite NDVI
                  </span>
                </div>
                <div className="text-3xl font-bold text-emerald-900">NDVI</div>
                <div className="text-xs text-muted-foreground mt-1">
                  Damage area analysis
                </div>
                <div className="mt-3 h-1.5 bg-emerald-100 rounded-full overflow-hidden">
                  <div className="h-full w-[65%] bg-gradient-to-r from-emerald-500 to-emerald-600 rounded-full" />
                </div>
              </motion.div>

              <motion.div
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.6, delay: 0.4 }}
                className="absolute -bottom-6 -left-6 bg-white rounded-2xl shadow-xl p-4 max-w-[260px] border border-amber-100"
              >
                <div className="flex items-center gap-2 text-amber-700 mb-2">
                  <CloudRain className="h-4 w-4" />
                  <span className="text-xs font-semibold uppercase tracking-wide">
                    IMD Weather
                  </span>
                </div>
                <div className="grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <div className="text-xs text-muted-foreground">Rainfall</div>
                    <div className="font-semibold text-amber-900">285mm</div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground">Humidity</div>
                    <div className="font-semibold text-amber-900">92%</div>
                  </div>
                </div>
              </motion.div>
            </div>

            <div>
              <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-emerald-100 text-emerald-700 text-xs font-medium uppercase tracking-widest mb-4">
                <Brain className="h-3.5 w-3.5" />
                The AI difference
              </div>
              <h2 className="font-serif text-4xl lg:text-5xl font-semibold leading-tight text-balance">
                We don&apos;t guess.
                <br />
                <span className="gradient-text">We verify, then pay.</span>
              </h2>
              <p className="mt-4 text-muted-foreground text-lg leading-relaxed">
                Traditional insurers send an agent to your farm weeks after the
                incident. We use satellite imagery, drone photos, and live weather
                data — fused by our damage detection model — to verify your claim
                objectively, in hours.
              </p>

              <div className="mt-8 space-y-4">
                {[
                  {
                    icon: Satellite,
                    title: "NDVI change detection",
                    desc: "We compare vegetation health before and after the incident over your exact plot.",
                  },
                  {
                    icon: CloudRain,
                    title: "IMD weather verification",
                    desc: "Rainfall, hail, frost and wind data pulled from the nearest weather station at the incident time.",
                  },
                  {
                    icon: Camera,
                    title: "Vision-model photo analysis",
                    desc: "Our model trained on 2.4M crop damage images classifies damage type and severity from your photos.",
                  },
                ].map((item, i) => (
                  <div key={i} className="flex gap-4">
                    <div className="h-10 w-10 shrink-0 rounded-xl bg-emerald-600 text-white grid place-items-center shadow-md shadow-emerald-600/30">
                      <item.icon className="h-5 w-5" />
                    </div>
                    <div>
                      <div className="font-semibold text-foreground">
                        {item.title}
                      </div>
                      <div className="text-sm text-muted-foreground mt-1">
                        {item.desc}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* TESTIMONIALS */}
      <section id="testimonials" className="py-24 lg:py-32">
        <div className="container mx-auto px-4">
          <SectionHeading
            eyebrow="Farmer Stories"
            title={
              <>
                Real farms. <span className="gradient-text">Real payouts.</span>
              </>
            }
            subtitle="Over ₹12.8 crore paid out in 2024. Here&apos;s what farmers say about their FarmClaim experience."
          />

          <div className="mt-16 grid lg:grid-cols-3 gap-6">
            {testimonials.map((t, i) => (
              <TestimonialCard key={i} t={t} />
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20">
        <div className="container mx-auto px-4">
          <div className="relative rounded-3xl overflow-hidden bg-emerald-900 text-white p-12 lg:p-20">
            <div className="absolute inset-0">
              <img
                src={HERO_IMG_2}
                alt=""
                className="w-full h-full object-cover opacity-20"
              />
              <div className="absolute inset-0 bg-gradient-to-br from-emerald-900 via-emerald-800/80 to-amber-900/60" />
            </div>
            <div className="relative z-10 max-w-2xl">
              <Award className="h-10 w-10 text-amber-300 mb-4" />
              <h2 className="font-serif text-4xl lg:text-5xl font-semibold leading-tight text-balance">
                Your harvest deserves a safety net built for this century.
              </h2>
              <p className="mt-4 text-emerald-100/80 text-lg">
                Join farmers who get paid in hours, not months. Free to
                sign up. No card required to browse plans.
              </p>
              <div className="mt-8 flex flex-col sm:flex-row gap-3">
                <Button
                  size="lg"
                  onClick={() => navigate("/signup")}
                  className="bg-amber-500 hover:bg-amber-400 text-emerald-950 font-semibold h-12 px-7 gap-2"
                >
                  Create your free account
                  <ArrowRight className="h-4 w-4" />
                </Button>
                <Button
                  size="lg"
                  variant="outline"
                  onClick={() => navigate("/login")}
                  className="bg-white/10 backdrop-blur text-white border-white/30 hover:bg-white/20 hover:text-white h-12 px-7"
                >
                  Sign in
                </Button>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* FAQ */}
      <section id="faq" className="py-24 lg:py-32 bg-muted/40">
        <div className="container mx-auto px-4 max-w-3xl">
          <SectionHeading
            eyebrow="Questions"
            title={
              <>
                Straight answers,
                <br />
                <span className="gradient-text">no fine print.</span>
              </>
            }
            subtitle=""
          />
          <div className="mt-12 space-y-3">
            {faqs.map((f, i) => (
              <FaqItem key={i} q={f.q} a={f.a} />
            ))}
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
}

function StatItem({
  value,
  label,
  icon: Icon,
}: {
  value: string;
  label: string;
  icon: React.ElementType;
}) {
  const ref = useRef(null);
  const inView = useInView(ref, { once: true });
  return (
    <div ref={ref} className="flex items-center gap-4">
      <div className="h-12 w-12 rounded-2xl bg-emerald-100 text-emerald-700 grid place-items-center shrink-0">
        <Icon className="h-6 w-6" />
      </div>
      <div>
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={inView ? { opacity: 1, y: 0 } : {}}
          transition={{ duration: 0.5 }}
          className="text-2xl lg:text-3xl font-bold text-foreground font-serif"
        >
          {value}
        </motion.div>
        <div className="text-sm text-muted-foreground">{label}</div>
      </div>
    </div>
  );
}

function SectionHeading({
  eyebrow,
  title,
  subtitle,
}: {
  eyebrow: string;
  title: React.ReactNode;
  subtitle: string;
}) {
  return (
    <div className="text-center max-w-2xl mx-auto">
      <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-emerald-100 text-emerald-700 text-xs font-medium uppercase tracking-widest mb-4">
        <Leaf className="h-3.5 w-3.5" />
        {eyebrow}
      </div>
      <h2 className="font-serif text-4xl lg:text-5xl font-semibold leading-tight text-balance">
        {title}
      </h2>
      {subtitle && (
        <p className="mt-4 text-muted-foreground text-lg text-pretty">
          {subtitle}
        </p>
      )}
    </div>
  );
}

function FeatureCard({
  icon: Icon,
  title,
  desc,
  accent,
  iconBg,
  index,
}: {
  icon: React.ElementType;
  title: string;
  desc: string;
  accent: string;
  iconBg: string;
  index: number;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.5, delay: index * 0.08 }}
      className="group relative"
    >
      <Card className="h-full overflow-hidden border-0 shadow-sm hover:shadow-2xl hover:shadow-emerald-900/10 transition-all duration-500 hover:-translate-y-1">
        <div className={cn("absolute inset-0 bg-gradient-to-br opacity-0 group-hover:opacity-100 transition-opacity duration-500", accent)} />
        <CardContent className="relative p-7">
          <div
            className={cn(
              "h-12 w-12 rounded-2xl text-white grid place-items-center shadow-lg mb-5",
              iconBg
            )}
          >
            <Icon className="h-6 w-6" strokeWidth={2} />
          </div>
          <h3 className="font-serif text-xl font-semibold mb-2">{title}</h3>
          <p className="text-muted-foreground text-sm leading-relaxed">{desc}</p>
        </CardContent>
      </Card>
    </motion.div>
  );
}

function StepRow({
  step,
  index,
}: {
  step: (typeof steps)[number];
  index: number;
}) {
  const ref = useRef(null);
  const inView = useInView(ref, { once: true, margin: "-100px" });
  const isRight = index % 2 === 1;

  return (
    <div
      ref={ref}
      className={cn(
        "relative lg:grid lg:grid-cols-2 lg:gap-12 items-center",
        isRight ? "lg:[&>*:first-child]:order-2" : ""
      )}
    >
      <motion.div
        initial={{ opacity: 0, x: isRight ? 30 : -30, y: 20 }}
        animate={inView ? { opacity: 1, x: 0, y: 0 } : {}}
        transition={{ duration: 0.6 }}
        className={cn("py-6 lg:py-12", isRight ? "lg:text-left lg:pl-12" : "lg:text-right lg:pr-12")}
      >
        <div className="flex items-center gap-3 mb-3 lg:justify-start">
          <span className="font-serif text-5xl font-bold text-emerald-400/30">
            {step.n}
          </span>
          <div className="h-10 w-10 rounded-xl bg-emerald-500/20 text-emerald-200 grid place-items-center">
            <step.icon className="h-5 w-5" />
          </div>
        </div>
        <h3 className="font-serif text-2xl font-semibold text-white mb-2">
          {step.title}
        </h3>
        <p className="text-emerald-100/70 max-w-md lg:max-w-sm leading-relaxed lg:inline-block">
          {step.desc}
        </p>
      </motion.div>
      <div className="hidden lg:block" />
    </div>
  );
}

function PlanCard({
  plan,
  onChoose,
}: {
  plan: (typeof plans)[number];
  onChoose: () => void;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.5 }}
      className="relative"
    >
      <Card
        className={cn(
          "h-full overflow-hidden border-0 shadow-md hover:shadow-2xl transition-all duration-500 hover:-translate-y-1 relative",
          plan.popular && "ring-2 ring-emerald-600"
        )}
      >
        {plan.popular && (
          <div className="absolute top-4 right-4">
            <Badge className="bg-emerald-700 text-white border-0">Most popular</Badge>
          </div>
        )}
        <div className={cn("h-2 bg-gradient-to-r", plan.color)} />
        <CardContent className="p-7">
          <div className="flex items-center gap-2 text-xs uppercase tracking-widest text-muted-foreground mb-1">
            <Sprout className="h-3.5 w-3.5" />
            {plan.crop}
          </div>
          <h3 className="font-serif text-2xl font-semibold mb-1">{plan.name}</h3>
          <div className="text-sm text-muted-foreground mb-5">{plan.provider}</div>

          <div className="flex items-baseline gap-1 mb-1">
            <span className="text-3xl font-bold text-foreground">
              {formatINR(plan.premium)}
            </span>
            <span className="text-sm text-muted-foreground">/ hectare</span>
          </div>
          <div className="text-sm text-muted-foreground mb-6">
            for {plan.duration}
          </div>

          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Sum insured / hectare</span>
              <span className="font-semibold">{formatINR(plan.coverage)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Coverage</span>
              <span className="font-semibold">{plan.coveragePct}% of damage</span>
            </div>
          </div>

          <Button
            onClick={onChoose}
            className="w-full mt-7 bg-emerald-700 hover:bg-emerald-800 text-white"
          >
            Choose this plan
          </Button>
        </CardContent>
      </Card>
    </motion.div>
  );
}

function TestimonialCard({ t }: { t: (typeof testimonials)[number] }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.5 }}
    >
      <Card className="h-full border-0 shadow-sm hover:shadow-lg transition-shadow duration-300">
        <CardContent className="p-7 flex flex-col h-full">
          <Quote className="h-8 w-8 text-emerald-300 mb-3" />
          <p className="text-foreground/90 leading-relaxed flex-1 text-pretty">
            &ldquo;{t.quote}&rdquo;
          </p>
          <div className="flex gap-0.5 mt-5 mb-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <Star key={i} className="h-4 w-4 fill-amber-400 text-amber-400" />
            ))}
          </div>
          <div className="flex items-center gap-3 pt-4 border-t">
            <img
              src={t.img}
              alt={t.name}
              className="h-11 w-11 rounded-full object-cover bg-emerald-100"
            />
            <div>
              <div className="font-semibold text-sm">{t.name}</div>
              <div className="text-xs text-muted-foreground">{t.location}</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
}

function FaqItem({ q, a }: { q: string; a: string }) {
  const [open, setOpen] = useState(false);
  return (
    <Card className="overflow-hidden border-0 shadow-sm">
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full text-left p-5 flex items-center justify-between gap-4"
      >
        <span className="font-semibold text-foreground">{q}</span>
        <ChevronDown
          className={cn(
            "h-5 w-5 text-muted-foreground transition-transform shrink-0",
            open && "rotate-180"
          )}
        />
      </button>
      <motion.div
        initial={false}
        animate={{ height: open ? "auto" : 0, opacity: open ? 1 : 0 }}
        transition={{ duration: 0.3 }}
        className="overflow-hidden"
      >
        <div className="px-5 pb-5 text-muted-foreground leading-relaxed">{a}</div>
      </motion.div>
    </Card>
  );
}

function Footer() {
  const navigate = useApp((s) => s.navigate);
  return (
    <footer className="bg-emerald-950 text-emerald-100/70 pt-16 pb-8 mt-auto">
      <div className="container mx-auto px-4">
        <div className="grid lg:grid-cols-4 gap-10 pb-10">
          <div className="lg:col-span-2">
            <div className="flex items-center gap-2.5 mb-4">
              <div className="h-9 w-9 rounded-xl bg-gradient-to-br from-emerald-500 to-green-600 grid place-items-center">
                <Leaf className="h-5 w-5 text-white" />
              </div>
              <span className="font-serif text-xl font-semibold text-white">
                FarmClaim
              </span>
            </div>
            <p className="text-sm max-w-sm leading-relaxed">
              AI-powered crop insurance for India&apos;s farmers. Built with
              satellite data, ML and a whole lot of respect for the people who
              feed us.
            </p>
            <div className="flex gap-3 mt-5">
              <Badge variant="outline" className="bg-emerald-900/50 text-emerald-200 border-emerald-700">
                Farmer-first
              </Badge>
              <Badge variant="outline" className="bg-emerald-900/50 text-emerald-200 border-emerald-700">
                AI-powered
              </Badge>
            </div>
          </div>
          {[
            {
              title: "Product",
              links: [
                { label: "Features", href: "#features" },
                { label: "Insurance Plans", href: "#plans" },
                { label: "How it works", href: "#how" },
                { label: "FAQ", href: "#faq" },
              ],
            },
            {
              title: "Account",
              links: [
                { label: "Sign up", href: "/signup" },
                { label: "Sign in", href: "/login" },
              ],
            },
          ].map((col) => (
            <div key={col.title}>
              <h4 className="text-white font-semibold mb-4 text-sm">{col.title}</h4>
              <ul className="space-y-2.5">
                {col.links.map((link) => (
                  <li key={link.label}>
                    <button
                      onClick={() => {
                        if (link.href.startsWith("#")) {
                          document
                            .querySelector(link.href)
                            ?.scrollIntoView({ behavior: "smooth" });
                        } else {
                          navigate(link.href);
                        }
                      }}
                      className="text-sm hover:text-white transition-colors text-left"
                    >
                      {link.label}
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <div className="border-t border-emerald-800 pt-6 text-xs">
          <div>© {new Date().getFullYear()} FarmClaim Insurance Technologies Pvt. Ltd.</div>
        </div>
      </div>
    </footer>
  );
}
