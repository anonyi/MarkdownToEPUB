# EPUB Generator

A lightweight EPUB generator implemented in C# .NET 8, supporting Markdown input and cover images.

## Features

- Generate EPUB 2.0 files from Markdown content
- Support for multiple chapters
- Add cover images to your EPUB
- No external dependencies required
- Simple and easy-to-use Console

## Requirements

- .NET 8.0 

## Installation

Clone this repository or download the source code:

git clone https://github.com/anonyi/MarkdownToEPUB.git


## Usage

Here's a basic example of how to use the EPUB Generator:

```csharp
var generator = new EpubGenerator();
generator.CreateEpub(
    "mybook.epub",
    "My Book Title",
    "Author Name",
    "path/to/chapters",
    "path/to/cover.jpg"
);
```

Make sure your chapter files are named numerically (e.g., 1.md, 2.md, 3.md) and located in the specified chapters directory.

## Project Structure
EpubGenerator.cs: Main class containing the EPUB generation logic
Chapter.cs: Class representing a book chapter
Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

## License
This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments
- Thanks to the EPUB open standard for making ebook creation accessible.
- Inspired by the need for a simple, dependency-free EPUB generator in C#.

## Contact
If you have any questions or suggestions, please open an issue or contact anony.yi@gmail.com.
