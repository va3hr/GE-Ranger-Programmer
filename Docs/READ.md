# X2212 Programmer

A Windows Forms application for programming and managing the Xicor X2212 256x4 bit Non-Volatile Static RAM (NOVRAM) via parallel port interface.

**Hardware Interface**: This programmer communicates with X2212 devices through a parallel port connection. It has been successfully tested on Windows 10 using a PCIe parallel port card, ensuring compatibility with modern systems that lack built-in parallel ports.

## Overview

The X2212 is a 256x4 bit NOVRAM device that combines static RAM with EEPROM backup storage. This programmer provides a user-friendly interface for reading, writing, and managing data on X2212 devices through a parallel port connection.

## Features

### Core Functionality
- **Read from X2212**: Retrieve all 256 nibbles (128 bytes) from the device
- **Write to X2212**: Program data to the device's volatile RAM
- **Verify**: Compare programmed data against expected values
- **Store to EEPROM**: Save volatile RAM contents to non-volatile EEPROM backup
- **Device Probe**: Test communication and verify X2212 presence

### File Operations
- **Open .RGR Files**: Load hex data from RGR format files
- **Save .RGR Files**: Export current data in RGR format
- **Automatic Settings**: Remember last used folder and LPT port settings

### Data Editing
- **Hex Grid Editor**: 16-channel view with 8 bytes per channel
- **Channel Mapping**: Logical channel order (Ch1-Ch16) mapped to EEPROM addresses
- **ASCII Display**: Real-time ASCII representation of hex data
- **Copy/Paste Operations**: Efficient data manipulation between channels
- **Multi-Selection**: Select multiple channels for batch operations
- **Undo Support**: Revert recent changes

### Advanced Features
- **Timing Calibration**: Automatically optimize communication timing for your hardware
- **Custom LPT Base Address**: Support for non-standard parallel port addresses
- **Comprehensive Logging**: Detailed operation logs with timestamps
- **Error Handling**: Robust error recovery and user feedback

## Channel Mapping

The application displays channels in logical order (Ch1-Ch16) but maps them to the correct EEPROM addresses:

| Channel | EEPROM Address | Display Order |
|---------|----------------|---------------|
| Ch1     | 0xE0          | Row 1         |
| Ch2     | 0xD0          | Row 2         |
| Ch3     | 0xC0          | Row 3         |
| Ch4     | 0xB0          | Row 4         |
| Ch5     | 0xA0          | Row 5         |
| Ch6     | 0x90          | Row 6         |
| Ch7     | 0x80          | Row 7         |
| Ch8     | 0x70          | Row 8         |
| Ch9     | 0x60          | Row 9         |
| Ch10    | 0x50          | Row 10        |
| Ch11    | 0x40          | Row 11        |
| Ch12    | 0x30          | Row 12        |
| Ch13    | 0x20          | Row 13        |
| Ch14    | 0x10          | Row 14        |
| Ch15    | 0x00          | Row 15        |
| Ch16    | 0xF0          | Row 16        |

Files are stored in address order (0x00 to 0xF0) but displayed in channel order for easier editing.

## Keyboard Shortcuts

### File Operations
- **Ctrl+O**: Open .RGR file
- **Ctrl+S**: Save As .RGR file
- **Ctrl+Z**: Undo last change

### Edit Operations
- **Ctrl+C**: Copy current row to clipboard
- **Ctrl+V**: Paste clipboard to selected rows
- **F2** or **Double-click**: Edit cell
- **Enter** or **Tab**: Confirm cell edit
- **Escape**: Cancel cell edit

### Selection Operations
- **Click**: Select single row
- **Ctrl+Click**: Toggle row selection (multi-select)
- **Shift+Click**: Select range of rows
- **Ctrl+A**: Select all rows (via Clear Selection menu)

## Hardware Requirements

- **Parallel Port**: Any parallel port address (fully configurable)
- **X2212 Device**: Connected via appropriate interface circuit
- **Driver**: inpoutx64.dll for port access (included)

### Supported LPT Base Addresses
The application supports any parallel port address. Common addresses include:
- 0x378 (LPT1 - standard)
- 0x278 (LPT2 - standard)
- 0x3BC (LPT1 on some older systems)
- 0xA800 (custom interface card)
- **Any custom address**: The LPT Base field is fully editable to accommodate different hardware configurations

**Note**: The LPT base address is configurable in the main interface and automatically saved to settings. This ensures the application works with standard parallel ports, custom interface cards, and specialized hardware implementations.

## Installation

### Prerequisites
1. **Windows 10/11** (x64 architecture)
2. **Parallel port** (built-in or PCIe card)
3. **Administrator privileges** (required for parallel port access)

### Driver Installation (inpoutx64.dll)

The application requires the inpoutx64.dll driver for parallel port access on modern Windows systems.

#### Option 1: Use Included Driver (Recommended)
1. Extract all application files to a folder
2. Ensure `inpoutx64.dll` is in the same folder as `X2212Programmer.exe`
3. Run the application as Administrator
4. The driver will be automatically loaded when needed

#### Option 2: System Installation
For system-wide installation of the inpout32 driver:

1. **Download** the latest inpout32 package from the official source
2. **Extract** the files to a temporary folder
3. **Copy** `inpoutx64.dll` to `C:\Windows\System32\`
4. **Copy** `inpout32.dll` to `C:\Windows\SysWOW64\` (for 32-bit compatibility)
5. **Restart** Windows to ensure driver registration

#### Driver Verification
To verify the driver is working:
1. Run X2212 Programmer as Administrator
2. The startup log should show "Driver initialized - port set to safe idle state"
3. If you see "Driver not found" warnings, check the DLL placement and admin privileges

### Application Setup
1. **Run as Administrator**: Right-click the executable and select "Run as administrator"
2. **Configure LPT Address**: Set your parallel port base address (default: 0xA800)
3. **Test Connection**: Use Device → Probe Device to verify hardware communication
4. **Calibrate Timing**: Run Device → Calibrate Timing for optimal performance

### Troubleshooting Driver Issues
- **"Access Denied"**: Ensure running as Administrator
- **"Driver not found"**: Verify inpoutx64.dll is present and accessible
- **"Port access failed"**: Check LPT base address configuration
- **Windows Security**: Some antivirus software may block low-level port access

## Usage

### Basic Operation
1. **Set LPT Base Address**: Enter your parallel port address (default: 0xA800)
2. **Probe Device**: Use Device → Probe Device to test communication
3. **Read Data**: Device → Read from X2212 to load current device contents
4. **Edit Data**: Click cells to edit hex values, use copy/paste for efficiency
5. **Write Data**: Device → Write to X2212 to program the device
6. **Store**: Device → Store to EEPROM to save to non-volatile memory

### File Operations
1. **Load File**: File → Open .RGR to load saved data
2. **Edit**: Modify data using the hex grid editor
3. **Save File**: File → Save As .RGR to export changes
4. **Program**: Write to device and store to EEPROM

### Copy/Paste Workflow
1. **Select Source**: Click on the row you want to copy
2. **Copy**: Right-click → Copy Row or Ctrl+C
3. **Select Destinations**: Ctrl+click multiple rows to select targets
4. **Paste**: Right-click → Paste to Selected Rows or Ctrl+V

## Timing Calibration

The application includes automatic timing calibration for optimal performance:

1. **Device → Calibrate Timing**: Runs automatic calibration
2. **Tests various timing values**: Finds minimum working timing
3. **Applies safe settings**: Uses 2x minimum for reliability
4. **Saves to INI file**: Settings persist between sessions

Default timing values work for most systems, but calibration can improve reliability with longer cables or slower hardware.

## Troubleshooting

### Communication Issues
- Verify parallel port address is correct
- Check hardware connections
- Run as Administrator
- Try timing calibration
- Ensure X2212 is powered and connected

### File Format Issues
- RGR files must contain exactly 256 hex characters (128 bytes)
- Invalid characters are ignored during parsing
- Use hex values 00-FF only

### Common Error Messages
- **"txtMessages is NULL"**: UI initialization error, restart application
- **"Invalid hex value"**: Enter valid hex (00-FF) in cells
- **"Device probe failed"**: Check hardware connections and LPT address
- **"Read/Write failed"**: Communication error, try calibration

## Technical Details

### Data Format
- **Internal Storage**: 128 bytes in address order (0x00-0xFF)
- **Display Format**: 16 channels × 8 bytes each
- **File Format**: RGR files contain 256 ASCII hex characters
- **EEPROM Format**: 256 nibbles stored as big-endian

### Communication Protocol
- **Parallel Port**: Uses data, status, and control registers
- **Timing**: Configurable setup, pulse, and hold times
- **Error Detection**: Read-back verification for all operations

## Settings File

Configuration is saved in `X2212Programmer.ini`:
```ini
LPTBase=0xA800
LastFolder=C:\YourPath
SetupTime=10
PulseWidth=10
HoldTime=5
StoreTime=25
```

## Dependencies

- **.NET Framework**: 6.0 or later
- **inpoutx64.dll**: Parallel port access driver
- **Windows**: 7/8/10/11 (x64)

## License

This software is provided as-is for educational and professional use with X2212 NOVRAM devices.
