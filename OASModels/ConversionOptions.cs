using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OASModels
{
    public enum DocType
    {
        DOCX = 1,
        PPTX = 2
    };

    //
    // Summary:
    //     Represents how revisions and markup in the output file can be displayed.
    public enum BalloonState
    {
        //
        // Summary:
        //     Show all revisions in balloons.
        AlwaysUse = 0,
        //
        // Summary:
        //     Show all revisions inline with the document text.
        Inline = 1,
        //
        // Summary:
        //     Show comments and formatting revision in balloons, all other revisions inline
        //     with the document text.
        OnlyCommentsAndFormatting = 2
    };

    //
    // Summary:
    //     Represents how bookmarks can be created in fixed format output.
    public enum FixedFormatBookmark
    {
        //
        // Summary:
        //     Do not generate bookmarks in the fixed format output.
        None = 0,
        //
        // Summary:
        //     Convert Word headings into bookmarks in the fixed format output.
        Headings = 1,
        //
        // Summary:
        //     Convert Word bookmarks into bookmarks in the fixed format output.
        Bookmarks = 2
    };

    //
    // Summary:
    //     Represents the output quality that can be used for fixed format output.
    public enum FixedFormatQuality
    {
        //
        // Summary:
        //     Optimize the output for printing.
        Standard = 0,
        //
        // Summary:
        //     Optimize the output for online reading.
        Minimum = 1
    };

    /*
    public enum PublishOption
    {
        Default = 0,
        Slides = 0,
        Outline = 1,
        Handout1 = 2,
        Handout2 = 3,
        Handout3 = 4,
        Handout4 = 5,
        Handout6 = 6,
        Handout9 = 7
    }
    */

    public class ConversionOptions
    {
        //
        // Summary:
        //     Only for Word documents. Gets or sets a value that indicates the visibility of markup balloons in the
        //     output file.
        public BalloonState BalloonState { get; set; }

        //
        // Summary:
        //     Gets or sets a value that indicates if fonts are bitmapped and included in the
        //     output file when they cannot be embedded.
        public bool BitmapEmbeddedFonts { get; set; }

        //
        // Summary:
        //     Only for Word documents. Gets or sets a value that indicates how bookmarks are saved into the output file.
        public FixedFormatBookmark Bookmarks { get; set; }

        //
        // Summary:
        //     Gets or sets a value that indicates if document properties are saved to the output
        //     file.
        public bool IncludeDocumentProperties { get; set; }

        //
        // Summary:
        //     Gets or sets a value that indicates if document structure tags are saved to the
        //     output file.
        public bool IncludeDocumentStructure { get; set; }

        //
        // Summary:
        //     Only for Word documents. Gets or sets a value that indicates the output quality.
        public FixedFormatQuality OutputQuality { get; set; }

        //
        // Summary:
        //     Gets or sets a value that indicates if PDF output should use the PDF/A format.
        //
        public bool UsePDFA { get; set; }

        //
        // Summary:
        //     Only for PowerPoint. Gets or sets start slide for conversion
        //
        //public uint startSlide { get; set; }

        //
        // Summary:
        //     Only for PowerPoint. Gets or sets end slide for conversion
        //public uint endSlide { get; set; }

        //
        // Summary:
        //     Only for PowerPoint. Gets or sets FrameSlides
        public bool FrameSlides { get; set; }

        //
        // Summary:
        //     Only for PowerPoint. Gets or sets IncludeHiddenSlides
        public bool IncludeHiddenSlides { get; set; }

        //
        // Summary:
        //     Only for PowerPoint. Gets or sets UseVerticalOrder
        public bool UseVerticalOrder { get; set; }

        //
        // Summary:
        //     Only for PowerPoint. Gets or sets PublishOption
        //public PublishOption PublishOption { get; set; }

        public ConversionOptions()
        {
            // default options
            BalloonState = BalloonState.AlwaysUse;
            BitmapEmbeddedFonts = true;
            Bookmarks = FixedFormatBookmark.None;
            IncludeDocumentProperties = true;
            IncludeDocumentStructure = true;
            OutputQuality = FixedFormatQuality.Standard;
            UsePDFA = false;
            FrameSlides = false;
            IncludeHiddenSlides = false;
            UseVerticalOrder = false;
        }
    }
}
