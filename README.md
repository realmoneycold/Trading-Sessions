# Trading Sessions Indicator for AMP Quantower

This repository contains a highly accurate Trading Sessions indicator designed exclusively for the AMP Quantower trading platform. It visually maps out the major global trading sessions (Tokyo, London, New York) directly onto your charts using beautiful, shaded bounding boxes that stick perfectly to the candlesticks.

## Features

- **Pixel-Perfect Alignment:** Built natively using the `IChartWindowCoordinatesConverter` to ensure that the session boxes anchor flawlessly to the high, low, open, and close times of the session, regardless of horizontal scrolling or vertical panning.
- **Timezone Reliability:** Uses native Eastern Standard Time (EST/EDT) conversion from UTC. This ensures session times never drift regardless of local computer settings.
- **Dynamic Bounding:** The boxes perfectly encapsulate the Highest High and Lowest Low of each respective session.

## Preview

![Trading Sessions Indicator](trading%20sessions.png)

## Installation

1. Download the `tradingsession.cs` file.
2. Open AMP Quantower.
3. Open the **Algo** window and click **Create New Indicator**.
4. Paste the code from `tradingsession.cs` into the editor.
5. Click **Compile**. 
6. Add the indicator to your chart!

## Customization

You can easily adjust the colors, opacity, and time ranges for each session (Tokyo, London, New York) directly within the Quantower indicator settings menu without modifying the code.
