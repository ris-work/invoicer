using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using common;
using Eto.Drawing;
using Eto.Forms;
using EtoFE;
using RV.InvNew.Common;

namespace CommonUi
{
    /// <summary>
    /// Custom control for editing Physical Maps.
    /// Handles loading BMP (Base64), drawing grids, and selecting regions.
    /// </summary>
    public class PhysicalMapEditorPanel : Panel
    {
        private Bitmap? _mapBitmap;
        private Drawable _mapDrawable;
        private TextBox _tbVerticalLines;
        private TextBox _tbHorizontalLines;
        private TextBox _tbRegionName;
        private Button _btnLoadMap;
        private Button _btnSaveMap;
        private Button _btnSaveRegion;
        private Button _btnSave;
        private Label _lblStatus;

        // State
        private PhysicalMap _currentMap;
        private List<MappedLocation> _locations;
        private (int Col, int Row)? _selectedCell;

        public PhysicalMapEditorPanel()
        {
            GlobalState.RefreshBAT();
            _currentMap = new PhysicalMap { MapId = 0, MapName = "World Map" };
            _locations = new List<MappedLocation>();
            InitializeComponents();

            // Load existing data from BAT if available
            LoadFromBat();
        }

        // New Method to load from GlobalState.BAT
        private void LoadFromBat()
        {
            if (GlobalState.BAT == null) return;

            // Find the World Map (ID 0)
            var mapData = GlobalState.BAT.Map.FirstOrDefault(m => m.MapId == 1);

            if (mapData != null)
            {
                _currentMap = mapData;

                // Load Image using existing helper
                if (!string.IsNullOrEmpty(_currentMap.Map))
                {
                    SetMap(_currentMap.Map);
                }

                // Update UI Grid Settings
                _tbVerticalLines.Text = _currentMap.VerticalGridlines.ToString();
                _tbHorizontalLines.Text = _currentMap.HorizontalGridlines.ToString();

                // Load Locations
                if (GlobalState.BAT.Locations != null)
                {
                    _locations = GlobalState.BAT.Locations
                        .Where(l => l.MapId == _currentMap.MapId)
                        .ToList();
                }

                _mapDrawable.Invalidate();
                Log("Map loaded from BAT.");
            }
            else
            {
                Log("No map found in BAT with ID 0.");
            }
        }

        private void InitializeComponents()
        {
            var layout = new DynamicLayout { Padding = 10, Spacing = new Size(5, 5) };

            var toolbar = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5 };

            _btnLoadMap = new Button { Text = "Load Map Image" };
            _btnLoadMap.Click += BtnLoadMap_Click;

            toolbar.Items.Add(new StackLayoutItem(_btnLoadMap));

            _tbVerticalLines = new TextBox { PlaceholderText = "Cols (Vert)", Width = 80 };
            _tbHorizontalLines = new TextBox { PlaceholderText = "Rows (Horiz)", Width = 80 };

            toolbar.Items.Add(new StackLayoutItem(_tbVerticalLines));
            toolbar.Items.Add(new StackLayoutItem(_tbHorizontalLines));

            _btnSaveMap = new Button { Text = "Update Grid" };
            _btnSaveMap.Click += BtnSaveMap_Click;

            toolbar.Items.Add(new StackLayoutItem(_btnSaveMap));
            toolbar.Items.Add(new StackLayoutItem(null, true));

            _btnSave = new Button { Text = "Save All" };
            _btnSave.Click += BtnSave_Click;
            toolbar.Items.Add(new StackLayoutItem(_btnSave));

            layout.Add(toolbar);

            _mapDrawable = new Drawable { BackgroundColor = Colors.Gray, Size = new Size(600, 400) };
            _mapDrawable.Paint += MapDrawable_Paint;
            _mapDrawable.MouseDown += MapDrawable_MouseDown;

            layout.Add(_mapDrawable, yscale: true);

            var regionLayout = new TableLayout(2, 3);

            regionLayout.Add(new Label { Text = "Selected Region:", VerticalAlignment = VerticalAlignment.Center }, 0, 0);
            _lblStatus = new Label { Text = "None" };
            regionLayout.Add(_lblStatus, 0, 1);

            regionLayout.Add(new Label { Text = "Region Name:", VerticalAlignment = VerticalAlignment.Center }, 1, 0);
            _tbRegionName = new TextBox { PlaceholderText = "e.g. Shelf A1" };
            regionLayout.Add(_tbRegionName, 1, 1);

            _btnSaveRegion = new Button { Text = "Save Region" };
            _btnSaveRegion.Click += BtnSaveRegion_Click;
            regionLayout.Add(_btnSaveRegion, 1, 2);

            layout.Add(regionLayout);

            Content = layout;
        }

        #region Map Management

        public void SetMap(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image)) return;

            try
            {
                var bytes = Convert.FromBase64String(base64Image);
                using (var stream = new MemoryStream(bytes))
                {
                    _mapBitmap = new Bitmap(stream);
                }
                _currentMap.Map = base64Image;
                _mapDrawable.Invalidate();
                Log("Map loaded successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error loading map: {ex.Message}", "Error", MessageBoxType.Error);
                Log($"Error loading map: {ex.Message}");
            }
        }

        private void BtnLoadMap_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog { Filters = { new FileFilter("BMP Files (*.bmp)", ".bmp"), new FileFilter("PNG Files (*.png)", ".png") } };
            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                try
                {
                    using (var stream = File.OpenRead(dialog.FileName))
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        var bytes = ms.ToArray();
                        var base64 = Convert.ToBase64String(bytes);
                        SetMap(base64);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxType.Error);
                }
            }
        }

        private void BtnSaveMap_Click(object sender, EventArgs e)
        {
            if (int.TryParse(_tbVerticalLines.Text, out int v) && v > 0)
                _currentMap.VerticalGridlines = v;
            if (int.TryParse(_tbHorizontalLines.Text, out int h) && h > 0)
                _currentMap.HorizontalGridlines = h;

            _mapDrawable.Invalidate();
            Log($"Map grid updated: {v} cols x {h} rows.");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Construct the Data Transfer Object
            var payload = new MapSaveDto
            {
                Map = _currentMap,
                Locations = _locations
            };

            // Send request to backend
            var req = SendAuthenticatedRequest<MapSaveDto, PhysicalMap>.Send(
                payload,
                "/SaveMap",
                true
            );

            if (req.Error == false)
            {
                // If this was a new map (ID 0), update the local ID with the one generated by the DB
                if (_currentMap.MapId == 0 && req.Out != null)
                {
                    _currentMap.MapId = req.Out.MapId;
                }

                MessageBox.Show(this, "Map saved successfully!", "Success", MessageBoxType.Information);
                Log($"Map saved successfully. Map ID: {_currentMap.MapId}");
            }
            else
            {
                MessageBox.Show(this, "Failed to save map.", "Error", MessageBoxType.Error);
                Log("Error saving map.");
            }
        }

        #endregion

        #region Drawing & Interaction

        private void MapDrawable_Paint(object sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;
            var rect = _mapDrawable.Bounds;

            if (_mapBitmap != null)
            {
                var aspect = _mapBitmap.Width / (float)_mapBitmap.Height;
                float drawWidth, drawHeight, drawX, drawY;

                if (rect.Width / aspect <= rect.Height)
                {
                    drawWidth = rect.Width;
                    drawHeight = rect.Width / aspect;
                    drawX = 0;
                    drawY = (rect.Height - drawHeight) / 2;
                }
                else
                {
                    drawHeight = rect.Height;
                    drawWidth = rect.Height * aspect;
                    drawX = (rect.Width - drawWidth) / 2;
                    drawY = 0;
                }

                graphics.DrawImage(_mapBitmap, drawX, drawY, drawWidth, drawHeight);
                _mapDrawable.Tag = new RectangleF(drawX, drawY, drawWidth, drawHeight);
            }
            else
            {
                using (var brush = new SolidBrush(Colors.LightGrey))
                using (var font = new Font(SystemFont.Default, 12))
                {
                    graphics.FillRectangle(brush, rect);
                    var size = graphics.MeasureString(font, "No Map Loaded");
                    var point = new PointF(rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
                    graphics.DrawText(font, Colors.Black, point, "No Map Loaded");
                }
                _mapDrawable.Tag = null;
                return;
            }

            if (_currentMap.VerticalGridlines > 0 && _currentMap.HorizontalGridlines > 0)
            {
                var drawRect = (RectangleF)_mapDrawable.Tag;
                var cellW = drawRect.Width / _currentMap.VerticalGridlines;
                var cellH = drawRect.Height / _currentMap.HorizontalGridlines;

                using (var pen = new Pen(Colors.Black, 1) { DashStyle = DashStyles.DashDotDot})
                {
                    for (int i = 1; i < _currentMap.VerticalGridlines; i++)
                    {
                        float x = drawRect.X + i * cellW;
                        graphics.DrawLine(pen, x, drawRect.Top, x, drawRect.Bottom);
                    }
                    for (int i = 1; i < _currentMap.HorizontalGridlines; i++)
                    {
                        float y = drawRect.Y + i * cellH;
                        graphics.DrawLine(pen, drawRect.Left, y, drawRect.Right, y);
                    }
                }

                using (var font = new Font(SystemFont.Default, 10))
                using (var brush = new SolidBrush(Colors.Yellow))
                using (var bgBrush = new SolidBrush(new Color(0, 0, 0, 0.5f)))
                {
                    foreach (var loc in _locations)
                    {
                        if (loc.HorizontalSection < _currentMap.HorizontalGridlines &&
                            loc.VerticalSection < _currentMap.VerticalGridlines)
                        {
                            float x = drawRect.X + loc.VerticalSection * cellW;
                            float y = drawRect.Y + loc.HorizontalSection * cellH;

                            var size = graphics.MeasureString(font, loc.Name);
                            graphics.FillRectangle(bgBrush, x, y, size.Width, size.Height);
                            graphics.DrawText(font, brush, x, y, loc.Name);
                        }
                    }
                }

                if (_selectedCell.HasValue)
                {
                    var (c, r) = _selectedCell.Value;
                    float x = drawRect.X + c * cellW;
                    float y = drawRect.Y + r * cellH;

                    using (var pen = new Pen(Colors.Red, 2))
                    using (var brush = new SolidBrush(new Color(1, 0, 0, 0.2f)))
                    {
                        graphics.FillRectangle(brush, x, y, cellW, cellH);
                        graphics.DrawRectangle(pen, x, y, cellW, cellH);
                    }
                }
            }
        }

        private void MapDrawable_MouseDown(object sender, MouseEventArgs e)
        {
            if (_mapDrawable.Tag is RectangleF drawRect &&
                _currentMap.VerticalGridlines > 0 &&
                _currentMap.HorizontalGridlines > 0)
            {
                float relX = e.Location.X - drawRect.X;
                float relY = e.Location.Y - drawRect.Y;

                if (relX >= 0 && relX <= drawRect.Width && relY >= 0 && relY <= drawRect.Height)
                {
                    int col = (int)((relX / drawRect.Width) * _currentMap.VerticalGridlines);
                    int row = (int)((relY / drawRect.Height) * _currentMap.HorizontalGridlines);

                    col = Math.Min(col, (int)_currentMap.VerticalGridlines - 1);
                    row = Math.Min(row, (int)_currentMap.HorizontalGridlines - 1);

                    // Handle Left Click (Select)
                    if (e.Buttons == MouseButtons.Primary)
                    {
                        SelectCell(col, row);
                    }
                    // Handle Right Click (Remove)
                    else if (e.Buttons == MouseButtons.Alternate)
                    {
                        RemoveRegion(col, row);
                    }
                }
            }
        }

        private void SelectCell(int col, int row)
        {
            _selectedCell = (col, row);

            var existing = _locations.FirstOrDefault(l => l.MapId == _currentMap.MapId &&
                                                        l.HorizontalSection == row &&
                                                        l.VerticalSection == col);

            if (existing != null)
            {
                _lblStatus.Text = $"Selected: Col {col}, Row {row} ({existing.Name})";
                _tbRegionName.Text = existing.Name;
            }
            else
            {
                _lblStatus.Text = $"Selected: Col {col}, Row {row} (New)";
                _tbRegionName.Text = "";
            }

            _mapDrawable.Invalidate();
        }

        private void RemoveRegion(int col, int row)
        {
            var existing = _locations.FirstOrDefault(l => l.MapId == _currentMap.MapId &&
                                                        l.HorizontalSection == row &&
                                                        l.VerticalSection == col);

            if (existing != null)
            {
                var result = MessageBox.Show(this, $"Remove region '{existing.Name}' at [{col}, {row}]?", "Confirm Remove", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    _locations.Remove(existing);

                    // Clear selection if we removed the selected one
                    if (_selectedCell.HasValue && _selectedCell.Value.Col == col && _selectedCell.Value.Row == row)
                    {
                        _selectedCell = null;
                        _lblStatus.Text = "None";
                        _tbRegionName.Text = "";
                    }

                    _mapDrawable.Invalidate();
                    Log($"Region removed: {existing.Name} at [{col}, {row}]");
                }
            }
            else
            {
                Log($"No region found at [{col}, {row}] to remove.");
            }
        }

        #endregion

        #region Region Management

        private void BtnSaveRegion_Click(object sender, EventArgs e)
        {
            if (!_selectedCell.HasValue) return;
            if (string.IsNullOrWhiteSpace(_tbRegionName.Text)) return;

            var (col, row) = _selectedCell.Value;

            var existing = _locations.FirstOrDefault(l => l.MapId == _currentMap.MapId &&
                                                        l.HorizontalSection == row &&
                                                        l.VerticalSection == col);

            if (existing != null)
            {
                existing.Name = _tbRegionName.Text;
            }
            else
            {
                _locations.Add(new MappedLocation
                {
                    Id = 0,
                    MapId = _currentMap.MapId,
                    Name = _tbRegionName.Text,
                    HorizontalSection = row,
                    VerticalSection = col
                });
            }

            _mapDrawable.Invalidate();
            Log($"Region saved: {_tbRegionName.Text} at [{col}, {row}]");
        }

        #endregion

        private void Log(string msg) => Console.WriteLine($"[PhysicalMapEditor] {msg}");
    }
}