#!/bin/bash
find "Werewolf for Telegram" -name "*.csproj" | while read -r csproj; do
  if ! grep -q '<Reference Include="netstandard" />' "$csproj"; then
    echo "Patching $csproj"
    sed -i '/<Reference Include="System" \/>/i \    <Reference Include="netstandard" />' "$csproj"
  fi
done
