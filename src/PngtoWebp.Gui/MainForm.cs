using PngtoWebp.Core;

namespace PngtoWebp.Gui;

public sealed class MainForm : Form
{
    private readonly ImageConverterService _converter = new();
    private readonly ListView _filesList = new();
    private readonly Button _addFilesButton = new();
    private readonly Button _removeSelectedButton = new();
    private readonly Button _clearButton = new();
    private readonly Button _browseOutputButton = new();
    private readonly Button _convertButton = new();
    private readonly Button _cancelButton = new();
    private readonly TextBox _outputTextBox = new();
    private readonly CheckBox _sameFolderCheckBox = new();
    private readonly CheckBox _autoConvertCheckBox = new();
    private readonly CheckBox _losslessCheckBox = new();
    private readonly CheckBox _overwriteCheckBox = new();
    private readonly NumericUpDown _qualityNumeric = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();

    private CancellationTokenSource? _conversionCancellation;
    private bool _isConverting;

    public MainForm()
    {
        Text = "PNG / JPG / BMP to WebP";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 620);
        Size = new Size(1080, 720);
        AllowDrop = true;

        BuildUi();
        WireEvents();
        UpdateUiState();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _conversionCancellation?.Cancel();
        base.OnFormClosing(e);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 8)
        };

        ConfigureButton(_addFilesButton, "Agregar archivos...", 145);
        ConfigureButton(_removeSelectedButton, "Quitar seleccionados", 150);
        ConfigureButton(_clearButton, "Limpiar lista", 110);

        actions.Controls.AddRange([
            _addFilesButton,
            _removeSelectedButton,
            _clearButton
        ]);

        _filesList.Dock = DockStyle.Fill;
        _filesList.View = View.Details;
        _filesList.FullRowSelect = true;
        _filesList.GridLines = true;
        _filesList.HideSelection = false;
        _filesList.MultiSelect = true;
        _filesList.Columns.Add("Archivo", 220);
        _filesList.Columns.Add("Carpeta", 340);
        _filesList.Columns.Add("Estado", 180);
        _filesList.Columns.Add("Salida", 260);

        var optionsGroup = new GroupBox
        {
            Text = "Opciones de conversión",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(10)
        };

        var options = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 3
        };

        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        options.Controls.Add(new Label
        {
            Text = "Carpeta de salida:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 6, 6, 0)
        }, 0, 0);

        _outputTextBox.Dock = DockStyle.Fill;
        options.Controls.Add(_outputTextBox, 1, 0);

        ConfigureButton(_browseOutputButton, "Examinar...", 100);
        options.Controls.Add(_browseOutputButton, 2, 0);

        _sameFolderCheckBox.Text = "Guardar junto al original";
        _sameFolderCheckBox.Checked = true;
        _sameFolderCheckBox.AutoSize = true;
        _sameFolderCheckBox.Anchor = AnchorStyles.Left;
        options.Controls.Add(_sameFolderCheckBox, 3, 0);

        options.Controls.Add(new Label
        {
            Text = "Calidad WebP:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 8, 6, 0)
        }, 0, 1);

        _qualityNumeric.Minimum = 0;
        _qualityNumeric.Maximum = 100;
        _qualityNumeric.Value = 80;
        _qualityNumeric.Width = 80;
        _qualityNumeric.Anchor = AnchorStyles.Left;
        options.Controls.Add(_qualityNumeric, 1, 1);

        var compressionOptions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };

        _losslessCheckBox.Text = "Lossless";
        _losslessCheckBox.AutoSize = true;
        _overwriteCheckBox.Text = "Sobrescribir existentes";
        _overwriteCheckBox.AutoSize = true;

        compressionOptions.Controls.Add(_losslessCheckBox);
        compressionOptions.Controls.Add(_overwriteCheckBox);
        options.Controls.Add(compressionOptions, 2, 1);
        options.SetColumnSpan(compressionOptions, 2);

        _autoConvertCheckBox.Text = "Convertir automáticamente al agregar archivos";
        _autoConvertCheckBox.Checked = true;
        _autoConvertCheckBox.AutoSize = true;
        options.Controls.Add(_autoConvertCheckBox, 0, 2);
        options.SetColumnSpan(_autoConvertCheckBox, 4);

        optionsGroup.Controls.Add(options);

        var progressPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 10, 0, 4)
        };

        progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 1;
        _progressBar.Height = 24;
        progressPanel.Controls.Add(_progressBar, 0, 0);

        ConfigureButton(_cancelButton, "Cancelar", 100);
        progressPanel.Controls.Add(_cancelButton, 1, 0);

        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 0)
        };

        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _statusLabel.Text = "Arrastrá imágenes aquí o usá “Agregar archivos...”.";
        _statusLabel.AutoSize = true;
        _statusLabel.Anchor = AnchorStyles.Left;
        bottomPanel.Controls.Add(_statusLabel, 0, 0);

        ConfigureButton(_convertButton, "Convertir archivos", 150);
        _convertButton.Height = 34;
        bottomPanel.Controls.Add(_convertButton, 1, 0);

        root.Controls.Add(actions, 0, 0);
        root.Controls.Add(_filesList, 0, 1);
        root.Controls.Add(optionsGroup, 0, 2);
        root.Controls.Add(progressPanel, 0, 3);
        root.Controls.Add(bottomPanel, 0, 4);

        Controls.Add(root);
    }

    private void WireEvents()
    {
        _addFilesButton.Click += async (_, _) => await SelectFilesAsync();
        _removeSelectedButton.Click += (_, _) => RemoveSelectedFiles();
        _clearButton.Click += (_, _) => ClearFiles();
        _browseOutputButton.Click += (_, _) => BrowseOutputDirectory();
        _convertButton.Click += async (_, _) => await StartConversionAsync();
        _cancelButton.Click += (_, _) => _conversionCancellation?.Cancel();
        _sameFolderCheckBox.CheckedChanged += (_, _) => UpdateUiState();
        _filesList.SelectedIndexChanged += (_, _) => UpdateUiState();
        DragEnter += MainForm_DragEnter;
        DragDrop += MainForm_DragDrop;
    }

    private async Task SelectFilesAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Seleccionar imágenes",
            Filter = "Imágenes compatibles|*.png;*.jpg;*.jpeg;*.bmp|PNG|*.png|JPG/JPEG|*.jpg;*.jpeg|BMP|*.bmp|Todos los archivos|*.*",
            Multiselect = true,
            CheckFileExists = true,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        AddFiles(dialog.FileNames);

        if (_autoConvertCheckBox.Checked && _filesList.Items.Count > 0)
        {
            await StartConversionAsync();
        }
    }

    private async void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (_isConverting || e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }

        AddFiles(paths);

        if (_autoConvertCheckBox.Checked && _filesList.Items.Count > 0)
        {
            await StartConversionAsync();
        }
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        if (_isConverting || e.Data is null || !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        e.Effect = DragDropEffects.Copy;
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var existing = _filesList.Items
            .Cast<ListViewItem>()
            .Select(item => item.Tag as string)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var ignored = 0;

        foreach (var rawPath in paths)
        {
            if (!File.Exists(rawPath) || !ImageConverterService.IsSupported(rawPath))
            {
                ignored++;
                continue;
            }

            var path = Path.GetFullPath(rawPath);
            if (!existing.Add(path))
            {
                continue;
            }

            var item = new ListViewItem(Path.GetFileName(path))
            {
                Tag = path
            };

            item.SubItems.Add(Path.GetDirectoryName(path) ?? string.Empty);
            item.SubItems.Add("Pendiente");
            item.SubItems.Add(string.Empty);
            _filesList.Items.Add(item);
            added++;
        }

        if (added > 0)
        {
            _statusLabel.Text = $"{added} archivo(s) agregado(s). Total: {_filesList.Items.Count}.";
        }
        else if (ignored > 0)
        {
            _statusLabel.Text = "No se agregaron archivos compatibles. Formatos permitidos: PNG, JPG/JPEG y BMP.";
        }

        UpdateUiState();
    }

    private void RemoveSelectedFiles()
    {
        foreach (ListViewItem item in _filesList.SelectedItems)
        {
            _filesList.Items.Remove(item);
        }

        _statusLabel.Text = $"Total de archivos: {_filesList.Items.Count}.";
        UpdateUiState();
    }

    private void ClearFiles()
    {
        _filesList.Items.Clear();
        _progressBar.Value = 0;
        _progressBar.Maximum = 1;
        _statusLabel.Text = "Lista vacía.";
        UpdateUiState();
    }

    private void BrowseOutputDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Seleccionar carpeta donde se guardarán los WebP",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (Directory.Exists(_outputTextBox.Text))
        {
            dialog.SelectedPath = _outputTextBox.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputTextBox.Text = dialog.SelectedPath;
        }
    }

    private async Task StartConversionAsync()
    {
        if (_isConverting || _filesList.Items.Count == 0)
        {
            return;
        }

        string? outputDirectory = null;
        if (!_sameFolderCheckBox.Checked)
        {
            outputDirectory = _outputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                MessageBox.Show(
                    this,
                    "Seleccioná una carpeta de salida o activá “Guardar junto al original”.",
                    "Carpeta de salida requerida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"No se pudo crear o acceder a la carpeta de salida.\n\n{ex.Message}",
                    "Error de carpeta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        _isConverting = true;
        _conversionCancellation = new CancellationTokenSource();
        UpdateUiState();

        var token = _conversionCancellation.Token;
        var options = new ConversionOptions
        {
            Quality = (int)_qualityNumeric.Value,
            Lossless = _losslessCheckBox.Checked,
            Overwrite = _overwriteCheckBox.Checked,
            OutputDirectory = outputDirectory
        };

        _progressBar.Minimum = 0;
        _progressBar.Maximum = Math.Max(1, _filesList.Items.Count);
        _progressBar.Value = 0;

        var successful = 0;
        var skipped = 0;
        var failed = 0;

        try
        {
            for (var index = 0; index < _filesList.Items.Count; index++)
            {
                token.ThrowIfCancellationRequested();

                var item = _filesList.Items[index];
                var inputPath = item.Tag as string
                    ?? throw new InvalidOperationException("La lista contiene un elemento sin ruta asociada.");

                item.SubItems[2].Text = "Convirtiendo...";
                _statusLabel.Text = $"Convirtiendo {index + 1} de {_filesList.Items.Count}: {Path.GetFileName(inputPath)}";
                item.EnsureVisible();

                var batch = await Task.Run(
                    () => _converter.ConvertAsync(inputPath, options, token),
                    token);

                var result = batch.Files.Single();
                item.SubItems[3].Text = result.OutputPath;

                switch (result.Status)
                {
                    case ConversionStatus.Success:
                        successful++;
                        item.SubItems[2].Text = $"OK · ahorro {result.SavingsPercentage:F1}%";
                        break;

                    case ConversionStatus.Skipped:
                        skipped++;
                        item.SubItems[2].Text = "Omitido · ya existe";
                        break;

                    case ConversionStatus.Failed:
                        failed++;
                        item.SubItems[2].Text = $"Error · {result.Message}";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _progressBar.Value = index + 1;
            }

            _statusLabel.Text = $"Finalizado. Convertidos: {successful} · Omitidos: {skipped} · Errores: {failed}.";

            if (failed == 0)
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            else
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = $"Conversión cancelada. Convertidos: {successful} · Omitidos: {skipped} · Errores: {failed}.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "La conversión terminó con un error inesperado.";
            MessageBox.Show(
                this,
                ex.Message,
                "Error durante la conversión",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _conversionCancellation.Dispose();
            _conversionCancellation = null;
            _isConverting = false;
            UpdateUiState();
        }
    }

    private void UpdateUiState()
    {
        var hasFiles = _filesList.Items.Count > 0;
        var customOutput = !_sameFolderCheckBox.Checked;

        _addFilesButton.Enabled = !_isConverting;
        _removeSelectedButton.Enabled = !_isConverting && _filesList.SelectedItems.Count > 0;
        _clearButton.Enabled = !_isConverting && hasFiles;
        _sameFolderCheckBox.Enabled = !_isConverting;
        _autoConvertCheckBox.Enabled = !_isConverting;
        _losslessCheckBox.Enabled = !_isConverting;
        _overwriteCheckBox.Enabled = !_isConverting;
        _qualityNumeric.Enabled = !_isConverting;
        _outputTextBox.Enabled = !_isConverting && customOutput;
        _browseOutputButton.Enabled = !_isConverting && customOutput;
        _convertButton.Enabled = !_isConverting && hasFiles;
        _cancelButton.Enabled = _isConverting;
    }

    private static void ConfigureButton(Button button, string text, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 30;
        button.AutoSize = false;
        button.Margin = new Padding(0, 0, 8, 0);
    }
}
