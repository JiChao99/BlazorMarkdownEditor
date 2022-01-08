using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using PSC.Blazor.Components.MarkdownEditor.Services;
using System.Net.Http;

namespace PSC.Blazor.Components.MarkdownEditor
{
    public partial class MarkdownEditor
    {
        private DotNetObjectReference<MarkdownEditor> dotNetObjectRef;
        /// <summary>
        /// Gets or sets the <see cref = "JSMarkdownInterop"/> instance.
        /// </summary>
        protected JSMarkdownInterop JSModule { get; private set; }

        private string ElementId { get; set; }

        private ElementReference ElementRef { get; set; }

        private List<MarkdownToolbarButton> toolbarButtons;
        /// <summary>
        /// Number of processed bytes in current file.
        /// </summary>
        protected long ProgressProgress;
        /// <summary>
        /// Total number of bytes in currently processed file.
        /// </summary>
        protected long ProgressTotal;
        /// <summary>
        /// Percentage of the current file-read status.
        /// </summary>
        protected double Progress;
        /// <inheritdoc/>
        protected bool ShouldAutoGenerateId => true;
        /// <summary>
        /// Indicates if markdown editor is properly initialized.
        /// </summary>
        protected bool Initialized { get; set; }

        /// <summary>
        /// Gets or sets the markdown value.
        /// </summary>
        [Parameter]
        public string Value { get; set; }

        /// <summary>
        /// An event that occurs after the markdown value has changed.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// If set to true, force downloads Font Awesome (used for icons). If set to false, prevents downloading.
        /// </summary>
        [Parameter]
        public bool? AutoDownloadFontAwesome { get; set; }

        /// <summary>
        /// If set to true, enables line numbers in the editor.
        /// </summary>
        [Parameter]
        public bool LineNumbers { get; set; }

        /// <summary>
        /// If set to false, disable line wrapping. Defaults to true.
        /// </summary>
        [Parameter]
        public bool LineWrapping { get; set; } = true;
        /// <summary>
        /// Sets the minimum height for the composition area, before it starts auto-growing.
        /// Should be a string containing a valid CSS value like "500px". Defaults to "300px".
        /// </summary>
        [Parameter]
        public string MinHeight { get; set; } = "300px";
        /// <summary>
        /// Sets fixed height for the composition area. minHeight option will be ignored.
        /// Should be a string containing a valid CSS value like "500px". Defaults to undefined.
        /// </summary>
        [Parameter]
        public string MaxHeight { get; set; }

        /// <summary>
        /// If set, displays a custom placeholder message.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; }

        /// <summary>
        /// If set, customize the tab size. Defaults to 2.
        /// </summary>
        [Parameter]
        public int TabSize { get; set; } = 2;
        /// <summary>
        /// Override the theme. Defaults to easymde.
        /// </summary>
        [Parameter]
        public string Theme { get; set; } = "easymde";
        /// <summary>
        /// rtl or ltr. Changes text direction to support right-to-left languages. Defaults to ltr.
        /// </summary>
        [Parameter]
        public string Direction { get; set; } = "ltr";
        /// <summary>
        /// An array of icon names to hide. Can be used to hide specific icons shown by default without
        /// completely customizing the toolbar.
        /// </summary>
        [Parameter]
        public string[] HideIcons { get; set; } = new[] { "side-by-side", "fullscreen" };
        /// <summary>
        /// An array of icon names to show. Can be used to show specific icons hidden by default without
        /// completely customizing the toolbar.
        /// </summary>
        [Parameter]
        public string[] ShowIcons { get; set; } = new[] { "code", "table" };
        /// <summary>
        /// [Optional] Gets or sets the content of the toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment Toolbar { get; set; }

        /// <summary>
        /// If set to false, disable toolbar button tips. Defaults to true.
        /// </summary>
        [Parameter]
        public bool ToolbarTips { get; set; } = true;
        /// <summary>
        /// Occurs after the custom toolbar button is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<MarkdownButtonEventArgs> CustomButtonClicked { get; set; }

        /// <summary>
        /// If set to true, enables the image upload functionality, which can be triggered by drag-drop,
        /// copy-paste and through the browse-file window (opened when the user click on the upload-image icon).
        /// Defaults to false.
        /// </summary>
        [Parameter]
        public bool UploadImage { get; set; }

        /// <summary>
        /// Gets or sets the max chunk size when uploading the file.
        /// </summary>
        [Parameter]
        public int MaxUploadImageChunkSize { get; set; } = 20 * 1024;
        /// <summary>
        /// Gets or sets the Segment Fetch Timeout when uploading the file.
        /// </summary>
        [Parameter]
        public TimeSpan SegmentFetchTimeout { get; set; } = TimeSpan.FromMinutes(1);
        /// <summary>
        /// Maximum image size in bytes, checked before upload (note: never trust client, always check image
        /// size at server-side). Defaults to 1024*1024*2 (2Mb).
        /// </summary>
        [Parameter]
        public long ImageMaxSize { get; set; } = 1024 * 1024 * 2;
        /// <summary>
        /// A comma-separated list of mime-types used to check image type before upload (note: never trust client, always
        /// check file types at server-side). Defaults to image/png, image/jpeg.
        /// </summary>
        [Parameter]
        public string ImageAccept { get; set; } = "image/png, image/jpeg";
        /// <summary>
        /// The endpoint where the images data will be sent, via an asynchronous POST request. The server is supposed to
        /// save this image, and return a json response.
        /// </summary>
        [Parameter]
        public string ImageUploadEndpoint { get; set; }

        /// <summary>
        /// If set to true, will treat imageUrl from imageUploadFunction and filePath returned from imageUploadEndpoint as
        /// an absolute rather than relative path, i.e. not prepend window.location.origin to it.
        /// </summary>
        [Parameter]
        public string ImagePathAbsolute { get; set; }

        /// <summary>
        /// CSRF token to include with AJAX call to upload image. For instance used with Django backend.
        /// </summary>
        [Parameter]
        public string ImageCSRFToken { get; set; }

        /// <summary>
        /// Texts displayed to the user (mainly on the status bar) for the import image feature, where
        /// #image_name#, #image_size# and #image_max_size# will replaced by their respective values, that
        /// can be used for customization or internationalization.
        /// </summary>
        [Parameter]
        public MarkdownImageTexts ImageTexts { get; set; }

        /// <summary>
        /// Occurs every time the selected image has changed.
        /// </summary>
        [Parameter]
        public Func<FileChangedEventArgs, Task> ImageUploadChanged { get; set; }

        /// <summary>
        /// Occurs when an individual image upload has started.
        /// </summary>
        [Parameter]
        public Func<FileStartedEventArgs, Task> ImageUploadStarted { get; set; }

        /// <summary>
        /// Occurs when an individual image upload has ended.
        /// </summary>
        [Parameter]
        public Func<FileEndedEventArgs, Task> ImageUploadEnded { get; set; }

        /// <summary>
        /// Occurs every time the part of image has being written to the destination stream.
        /// </summary>
        [Parameter]
        public Func<FileWrittenEventArgs, Task> ImageUploadWritten { get; set; }

        /// <summary>
        /// Notifies the progress of image being written to the destination stream.
        /// </summary>
        [Parameter]
        public Func<FileProgressedEventArgs, Task> ImageUploadProgressed { get; set; }

        /// <summary>
        /// Errors displayed to the user, using the errorCallback option, where #image_name#, #image_size#
        /// and #image_max_size# will replaced by their respective values, that can be used for customization
        /// or internationalization.
        /// </summary>
        [Parameter]
        public MarkdownErrorMessages ErrorMessages { get; set; }

        /// <summary>
        /// A callback function used to define how to display an error message. Defaults to (errorMessage) => alert(errorMessage).
        /// </summary>
        [Parameter]
        public Func<string, Task> ErrorCallback { get; set; }

        protected override void OnInitialized()
        {
            if (JSModule == null)
            {
                JSModule = new JSMarkdownInterop(JSRuntime);
            }

            ElementId = $"markdown-{Guid.NewGuid()}";
            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                dotNetObjectRef ??= DotNetObjectReference.Create(this);

                await JSModule.Initialize(dotNetObjectRef, ElementRef, ElementId, new
                {
                    Value,
                    AutoDownloadFontAwesome,
                    HideIcons,
                    ShowIcons,
                    LineNumbers,
                    LineWrapping,
                    MinHeight,
                    MaxHeight,
                    Placeholder,
                    TabSize,
                    Theme,
                    Direction,
                    Toolbar = Toolbar != null && toolbarButtons?.Count > 0 ? MarkdownActionProvider.Serialize(toolbarButtons) : null,
                    ToolbarTips,
                    UploadImage,
                    ImageMaxSize,
                    ImageAccept,
                    ImageUploadEndpoint,
                    ImagePathAbsolute,
                    ImageCSRFToken,
                    ImageTexts = ImageTexts == null ? null : new
                    {
                        SbInit = ImageTexts.Init,
                        SbOnDragEnter = ImageTexts.OnDragEnter,
                        SbOnDrop = ImageTexts.OnDrop,
                        SbProgress = ImageTexts.Progress,
                        SbOnUploaded = ImageTexts.OnUploaded,
                        ImageTexts.SizeUnits,
                    },
                    ErrorMessages,
                });

                Initialized = true;
            }
        }

        /// <summary>
        /// Adds the custom toolbar button.
        /// </summary>
        /// <param name = "toolbarButton">Button instance.</param>
        internal protected void AddMarkdownToolbarButton(MarkdownToolbarButton toolbarButton)
        {
            toolbarButtons ??= new();
            toolbarButtons.Add(toolbarButton);
        }

        /// <summary>
        /// Removes the custom toolbar button.
        /// </summary>
        /// <param name = "toolbarButton">Button instance.</param>
        internal protected void RemoveMarkdownToolbarButton(MarkdownToolbarButton toolbarButton)
        {
            toolbarButtons.Remove(toolbarButton);
        }

        /// <summary>
        /// Gets the markdown value.
        /// </summary>
        /// <returns>Markdown value.</returns>
        public async Task<string> GetValueAsync()
        {
            if (!Initialized)
                return null;
            return await JSModule.GetValue(ElementId);
        }

        /// <summary>
        /// Sets the markdown value.
        /// </summary>
        /// <param name = "value">Value to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SetValueAsync(string value)
        {
            if (!Initialized)
                return;
            await JSModule.SetValue(ElementId, value);
        }

        /// <summary>
        /// Updates the internal markdown value. This method should only be called internally!
        /// </summary>
        /// <param name = "value">New value.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [JSInvokable]
        public Task UpdateInternalValue(string value)
        {
            Value = value;
            return ValueChanged.InvokeAsync(Value);
        }

        /// <summary>
        /// Notifies the component that file input value has changed.
        /// </summary>
        /// <param name = "file">Changed file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [JSInvokable]
        public async Task NotifyImageUpload(FileEntry file)
        {
            if (ImageUploadChanged is not null)
                await ImageUploadChanged.Invoke(new(file));

            await InvokeAsync(StateHasChanged);
        }

        public Task UpdateFileStartedAsync(FileEntry fileEntry)
        {
            // reset all
            ProgressProgress = 0;
            ProgressTotal = fileEntry.size;
            Progress = 0;

            if (ImageUploadStarted is not null)
                return ImageUploadStarted.Invoke(new(fileEntry));

            return Task.CompletedTask;
        }

        public async Task UpdateFileEndedAsync(FileEntry fileEntry, bool success, string fileInvalidReason)
        {
            if (ImageUploadEnded is not null)
                await ImageUploadEnded.Invoke(new(fileEntry, success, fileInvalidReason));

            if (success)
                await JSModule.NotifyImageUploadSuccess(ElementId, fileEntry.UploadUrl);
            else
                await JSModule.NotifyImageUploadError(ElementId, fileEntry.ErrorMessage);
        }

        public Task UpdateFileWrittenAsync(FileEntry fileEntry, long position, byte[] data)
        {
            if (ImageUploadWritten is not null)
                return ImageUploadWritten.Invoke(new(fileEntry, position, data));

            return Task.CompletedTask;
        }

        public Task UpdateFileProgressAsync(FileEntry fileEntry, long progressProgress)
        {
            ProgressProgress += progressProgress;

            var progress = Math.Round((double)ProgressProgress / ProgressTotal, 3);

            if (Math.Abs(progress - Progress) > double.Epsilon)
            {
                Progress = progress;

                if (ImageUploadProgressed is not null)
                    return ImageUploadProgressed.Invoke(new(fileEntry, Progress));
            }

            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task NotifyErrorMessage(string errorMessage)
        {
            if (ErrorCallback is not null)
                return ErrorCallback.Invoke(errorMessage);

            return Task.CompletedTask;
        }
    }
}