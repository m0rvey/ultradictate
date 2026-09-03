// swift-tools-version: 6.0
//
// SuperDictate — a Swift push-to-talk dictation app
// for macOS Apple Silicon. Native AppKit / AVFoundation, FluidAudio
// driving Parakeet TDT v3 on the Apple Neural Engine. macOS 14
// (Sonoma) minimum. The Hardened Runtime microphone entitlement
// (`com.apple.security.device.audio-input` in `entitlements.plist`)
// is what Tahoe 26 checks before exposing the app in Privacy &
// Security → Microphone; on macOS 14–25 the legacy sandbox key
// (`com.apple.security.device.microphone`) is the fallback. Both
// ship in the same build so a single notarised binary works
// across the supported range.
import PackageDescription

let package = Package(
    name: "UltraDictate",
    platforms: [
        .macOS("14.0"),
    ],
    products: [
        .executable(name: "UltraDictate", targets: ["UltraDictate"]),
    ],
    dependencies: [
        .package(path: "FluidAudio"),
    ],
    targets: [
        .executableTarget(
            name: "UltraDictate",
            dependencies: [
                .product(name: "FluidAudio", package: "FluidAudio"),
            ]
        ),
    ]
)
