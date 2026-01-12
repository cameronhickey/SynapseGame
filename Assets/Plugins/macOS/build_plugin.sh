#!/bin/bash

# Build script for MacOSSpeechPlugin
# This compiles the Objective-C plugin into a bundle that Unity can load

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SOURCE_FILE="$SCRIPT_DIR/MacOSSpeechPlugin.mm"
OUTPUT_BUNDLE="$SCRIPT_DIR/MacOSSpeechPlugin.bundle"

echo "Building MacOSSpeechPlugin..."
echo "Source: $SOURCE_FILE"
echo "Output: $OUTPUT_BUNDLE"

# Remove old bundle if exists
rm -rf "$OUTPUT_BUNDLE"

# Create bundle directory structure
mkdir -p "$OUTPUT_BUNDLE/Contents/MacOS"

# Compile the plugin
clang++ -shared -fPIC \
    -framework Foundation \
    -framework Speech \
    -framework AVFoundation \
    -framework AudioToolbox \
    -std=c++11 \
    -arch arm64 \
    -arch x86_64 \
    -mmacosx-version-min=10.15 \
    -o "$OUTPUT_BUNDLE/Contents/MacOS/MacOSSpeechPlugin" \
    "$SOURCE_FILE"

if [ $? -eq 0 ]; then
    # Create Info.plist
    cat > "$OUTPUT_BUNDLE/Contents/Info.plist" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>MacOSSpeechPlugin</string>
    <key>CFBundleIdentifier</key>
    <string>com.cerebrum.macosspeechplugin</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>MacOSSpeechPlugin</string>
    <key>CFBundlePackageType</key>
    <string>BNDL</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
</dict>
</plist>
EOF
    
    echo "✅ Build successful: $OUTPUT_BUNDLE"
    echo ""
    echo "Next steps:"
    echo "1. Refresh Unity project (Assets > Refresh)"
    echo "2. Rebuild the macOS app"
else
    echo "❌ Build failed!"
    exit 1
fi
