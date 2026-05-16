#!/usr/bin/env bash
# One-time Linux setup for the HotForge input backend.
#
# The backend reads /dev/input/event* and writes /dev/uinput. Out of the box
# both are root-only, which is why "Run" reports:
#     backend unavailable: No readable keyboard devices under /dev/input
#
# This grants your user persistent access. Run once:  ./scripts/setup-linux.sh
# Then open a NEW shell (or `newgrp input`) so the group change takes effect.

set -euo pipefail

if [[ $EUID -eq 0 ]]; then
    echo "Run as your normal user (the script calls sudo itself)." >&2
    exit 1
fi

user="${SUDO_USER:-$USER}"
echo "Configuring HotForge keyboard access for: $user"

# 1. Read access to keyboards (/dev/input/event* are group 'input').
sudo usermod -aG input "$user"

# 2. uinput virtual keyboard: load now and on every boot.
sudo modprobe uinput
echo uinput | sudo tee /etc/modules-load.d/uinput.conf >/dev/null

# 3. Let the 'input' group use /dev/uinput, now and persistently.
echo 'KERNEL=="uinput", GROUP="input", MODE="0660", OPTIONS+="static_node=uinput"' \
    | sudo tee /etc/udev/rules.d/99-uinput.rules >/dev/null
sudo udevadm control --reload-rules
sudo udevadm trigger /dev/uinput || true
sudo chgrp input /dev/uinput && sudo chmod 660 /dev/uinput

echo
echo "Done. Group membership applies to NEW logins only."
echo "To use it immediately without logging out:"
echo
echo "    newgrp input"
echo "    dotnet run --project src/HotForge.Gui"
