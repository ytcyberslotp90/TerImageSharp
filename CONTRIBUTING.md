# Contributing to TerImageSharp

First off, thank you for taking the time to contribute! 🎉 

`TerImageSharp` was built to provide uncompromising, pixel-perfect terminal graphics without relying on character-art approximations[cite: 1]. Contributions that optimize performance, fix protocol edge-cases, or expand terminal compatibility are incredibly welcome.

---

## Code Principles

Before writing any code, please keep our core philosophy in mind:
*   **No Glyphs / Braille Fallbacks:** We explicitly do not support rendering images via text blocks, ASCII, or Braille[cite: 1]. If a terminal cannot handle Sixel or Kitty graphics natively, the engine should gracefully inform the user rather than degrade to text art[cite: 1].
*   **Performance is Key:** Rendering real-time video frames (like live camera pipelines) or high-frame-rate GIFs requires high-throughput data processing[cite: 3]. Keep memory allocations inside the rendering loops to an absolute minimum.

---

## How to Contribute

### 1. Reporting Bugs
*   Check the Issues tab to ensure the bug hasn’t already been reported.
*   When opening an issue, please include:
    *   Your operating system and **exact terminal emulator** (e.g., Windows Terminal, Ghostty, WezTerm)[cite: 1, 8].
    *   The command options used[cite: 1].
    *   If applicable, the specific image file causing the issue.

### 2. Suggesting Enhancements
*   Open an issue describing the feature, why it’s useful, and how it aligns with the project's pixel-perfect philosophy[cite: 1].

### 3. Submitting Pull Requests
*   Fork the repository and create your branch from `main`.
*   Ensure your code matches the existing C# / .NET 10 styling across the project (implicit types where clear, file-scoped namespaces, clean layout)[cite: 1].
*   Run a local release build to verify your modifications before opening the PR[cite: 3]:
    ```bash
    dotnet build -c Release
    ```
*   Provide a clear description in your Pull Request explaining what changed and how you tested it.

---

## Development Roadmap Ideas

If you are looking for somewhere to start, here are a few areas we'd love help with:
*   **Auto-Fit Support:** Implementing an option to automatically scale images down to the active terminal window columns/rows dimensions if they exceed them.
*   **Live DA1 Queries:** Replacing or supplementing environment-variable checks in `TerminalCapabilities.cs` with a direct Device Attributes (DA1) query sequence to dynamically check for Sixel support[cite: 1, 8].
*   **Performance Tuning:** Optimizing the run-length encoding logic inside `SixelEncoder.cs` or base64 chunking in `KittyEncoder.cs` to maximize throughput[cite: 1, 4, 7].
