using MarkdownToEPUB;


var generator = new EpubGenerator();
generator.CreateEpub(
    "mybook.epub",
    "My Book Title",
    "Author Name",
    "Chapters",
    "cover.jpg"
);
