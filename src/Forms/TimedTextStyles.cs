﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Nikse.SubtitleEdit.Logic;
using Nikse.SubtitleEdit.Logic.SubtitleFormats;

namespace Nikse.SubtitleEdit.Forms
{

    public sealed partial class TimedTextStyles : Form
    {
        private Subtitle _subtitle = null;
        private XmlDocument _xml;
        private XmlNode _xmlHead;
        private XmlNamespaceManager _nsmgr;
        private bool _doUpdate = false;
        private Timer _previewTimer = new Timer();
        public string Header { get { return _xml.OuterXml; } }

        public TimedTextStyles(Subtitle subtitle)
        {
            InitializeComponent();

            _subtitle = subtitle;
            _xml = new XmlDocument();
            try
            {
                _xml.LoadXml(subtitle.Header);
                var xnsmgr = new XmlNamespaceManager(_xml.NameTable);
                xnsmgr.AddNamespace("ttml", "http://www.w3.org/ns/ttml");
                if (_xml.DocumentElement.SelectSingleNode("ttml:head", xnsmgr) == null)
                    _xml.LoadXml(new TimedText10().ToText(new Subtitle(), "tt")); // load default xml
            }
            catch
            {
                _xml.LoadXml(new TimedText10().ToText(new Subtitle(), "tt")); // load default xml
            }
            _nsmgr = new XmlNamespaceManager(_xml.NameTable);
            _nsmgr.AddNamespace("ttml", "http://www.w3.org/ns/ttml");
            _xmlHead = _xml.DocumentElement.SelectSingleNode("ttml:head", _nsmgr);

            foreach (FontFamily ff in FontFamily.Families)
                comboBoxFontName.Items.Add(ff.Name.Substring(0, 1).ToLower() + ff.Name.Substring(1));

            InitializeListView();

            _previewTimer.Interval = 200;
            _previewTimer.Tick += RefreshTimerTick;
        }

        void RefreshTimerTick(object sender, EventArgs e)
        {
            _previewTimer.Stop();
            GeneratePreviewReal();
        }

        private void GeneratePreviewReal()
        {
            if (listViewStyles.SelectedItems.Count != 1)
                return;

            if (pictureBoxPreview.Image != null)
                pictureBoxPreview.Image.Dispose();
            var bmp = new Bitmap(pictureBoxPreview.Width, pictureBoxPreview.Height);

            using (Graphics g = Graphics.FromImage(bmp))
            {

                // Draw background
                const int rectangleSize = 9;
                for (int y = 0; y < bmp.Height; y += rectangleSize)
                {
                    for (int x = 0; x < bmp.Width; x += rectangleSize)
                    {
                        Color c = Color.DarkGray;
                        if (y % (rectangleSize * 2) == 0)
                        {
                            if (x % (rectangleSize * 2) == 0)
                                c = Color.LightGray;
                        }
                        else
                        {
                            if (x % (rectangleSize * 2) != 0)
                                c = Color.LightGray;
                        }
                        g.FillRectangle(new SolidBrush(c), x, y, rectangleSize, rectangleSize);
                    }
                }

                // Draw text
                Font font;
                try
                {
                    double fontSize = 20;
                    if (Utilities.IsInteger(textBoxFontSize.Text.Replace("px", string.Empty)))
                    {
                        fontSize = Convert.ToInt32(textBoxFontSize.Text.Replace("px", string.Empty));
                    }
                    else if (textBoxFontSize.Text.EndsWith("%"))
                    {
                        int num;
                        if (int.TryParse(textBoxFontSize.Text.TrimEnd('%'), out num))
                            fontSize = fontSize * num / 100.0;
                    }
                    font = new Font(comboBoxFontName.Text, (float)fontSize);
                }
                catch
                {
                    font = new Font(Font, FontStyle.Regular);
                }
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
                var path = new GraphicsPath();

                bool newLine = false;
                var sb = new StringBuilder();
                sb.Append("This is a test!");

                var measuredWidth = TextDraw.MeasureTextWidth(font, sb.ToString(), comboBoxFontWeight.Text == "bold") + 1;
                var measuredHeight = TextDraw.MeasureTextHeight(font, sb.ToString(), comboBoxFontWeight.Text == "bold") + 1;

                float left = 5;
                //if (radioButtonTopLeft.Checked || radioButtonMiddleLeft.Checked || radioButtonBottomLeft.Checked)
                //    left = (float)numericUpDownMarginLeft.Value;
                //else if (radioButtonTopRight.Checked || radioButtonMiddleRight.Checked || radioButtonBottomRight.Checked)
                //    left = bmp.Width - (measuredWidth + ((float)numericUpDownMarginRight.Value));
                //else
                //    left = ((float)(bmp.Width - measuredWidth * 0.8 + 15) / 2);


                float top = 2;
                //if (radioButtonTopLeft.Checked || radioButtonTopCenter.Checked || radioButtonTopRight.Checked)
                //    top = (float)numericUpDownMarginVertical.Value;
                //else if (radioButtonMiddleLeft.Checked || radioButtonMiddleCenter.Checked || radioButtonMiddleRight.Checked)
                //    top = (bmp.Height - measuredHeight) / 2;
                //else
                //    top = bmp.Height - measuredHeight - ((int)numericUpDownMarginVertical.Value);

                int leftMargin = 0;
                int pathPointsStart = -1;

                //if (radioButtonOpaqueBox.Checked)
                //{
                //    if (_isSubStationAlpha)
                //        g.FillRectangle(new SolidBrush(panelBackColor.BackColor), left, top, measuredWidth + 3, measuredHeight + 3);
                //    else
                //        g.FillRectangle(new SolidBrush(panelOutlineColor.BackColor), left, top, measuredWidth + 3, measuredHeight + 3);
                //}

                TextDraw.DrawText(font, sf, path, sb, comboBoxFontStyle.Text == "italic", comboBoxFontWeight.Text == "bold", false, left, top, ref newLine, leftMargin, ref pathPointsStart);

                int outline = 0; // (int)numericUpDownOutline.Value;

                if (outline > 0)
                {
                    Color outlineColor = Color.White;
                    g.DrawPath(new Pen(outlineColor, outline), path);
                }
                g.FillPath(new SolidBrush(panelFontColor.BackColor), path);
            }
            pictureBoxPreview.Image = bmp;
        }

        private void InitializeListView()
        {
            XmlNode head = _xml.DocumentElement.SelectSingleNode("ttml:head", _nsmgr);
            foreach (XmlNode node in head.SelectNodes("//ttml:style", _nsmgr))
            {
                string name = "default";
                if (node.Attributes["xml:id"] != null)
                    name = node.Attributes["xml:id"].Value;
                else if (node.Attributes["id"] != null)
                    name = node.Attributes["id"].Value;

                string fontFamily = "Arial";
                if (node.Attributes["tts:fontFamily"] != null)
                    fontFamily = node.Attributes["tts:fontFamily"].Value;

                string fontWeight = "normal";
                if (node.Attributes["tts:fontWeight"] != null)
                    fontWeight = node.Attributes["tts:fontWeight"].Value;

                string fontStyle = "normal";
                if (node.Attributes["tts:fontStyle"] != null)
                    fontStyle = node.Attributes["tts:fontStyle"].Value;

                string fontColor = "white";
                if (node.Attributes["tts:color"] != null)
                    fontColor = node.Attributes["tts:color"].Value;

                string fontSize = "100%";
                if (node.Attributes["tts:fontSize"] != null)
                    fontSize = node.Attributes["tts:fontSize"].Value;

                AddStyle(name, fontFamily, fontColor, fontSize);
            }
            if (listViewStyles.Items.Count > 0)
                listViewStyles.Items[0].Selected = true;
        }

        private void AddStyle(string name, string fontFamily, string color, string fontSize)
        {
            var item = new ListViewItem(name.Trim());
            item.UseItemStyleForSubItems = false;

            var subItem = new ListViewItem.ListViewSubItem(item, fontFamily);
            item.SubItems.Add(subItem);

            subItem = new ListViewItem.ListViewSubItem(item, fontSize);
            item.SubItems.Add(subItem);

            subItem = new ListViewItem.ListViewSubItem(item, string.Empty);
            subItem.Text = color;
            Color c = Color.White;
            try
            {
                if (color.StartsWith("rgb("))
                {
                    string[] arr = color.Remove(0, 4).TrimEnd(')').Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    c = Color.FromArgb(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                }
                else
                {
                    c = System.Drawing.ColorTranslator.FromHtml(color);
                }
            }
            catch
            {
            }
            subItem.BackColor = c;
            item.SubItems.Add(subItem);

            int count = 0;
            foreach (Paragraph p in _subtitle.Paragraphs)
            {
                if (string.IsNullOrEmpty(p.Extra) && name.Trim() == "Default")
                    count++;
                else if (p.Extra != null && name.Trim().ToLower() == p.Extra.TrimStart().ToLower())
                    count++;
            }
            subItem = new ListViewItem.ListViewSubItem(item, count.ToString());
            item.SubItems.Add(subItem);

            listViewStyles.Items.Add(item);
        }

        private void TimedTextStyles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                DialogResult = DialogResult.Cancel;
        }

        private void listViewStyles_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            _doUpdate = false;
            if (listViewStyles.SelectedItems.Count == 1)
            {
                string styleName = listViewStyles.SelectedItems[0].Text;
                LoadStyle(styleName);
                GeneratePreview();
            }
            _doUpdate = true;
        }

        private void LoadStyle(string styleName)
        {
            XmlNode head = _xml.DocumentElement.SelectSingleNode("ttml:head", _nsmgr);
            foreach (XmlNode node in head.SelectNodes("//ttml:style", _nsmgr))
            {
                string name = "default";
                if (node.Attributes["xml:id"] != null)
                    name = node.Attributes["xml:id"].Value;
                else if (node.Attributes["id"] != null)
                    name = node.Attributes["id"].Value;
                if (name == styleName)
                {
                    string fontFamily = "Arial";
                    if (node.Attributes["tts:fontFamily"] != null)
                        fontFamily = node.Attributes["tts:fontFamily"].Value;

                    string fontWeight = "normal";
                    if (node.Attributes["tts:fontWeight"] != null)
                        fontWeight = node.Attributes["tts:fontWeight"].Value;

                    string fontStyle = "normal";
                    if (node.Attributes["tts:fontStyle"] != null)
                        fontStyle = node.Attributes["tts:fontStyle"].Value;

                    string fontColor = "white";
                    if (node.Attributes["tts:color"] != null)
                        fontColor = node.Attributes["tts:color"].Value;

                    string fontSize = "100%";
                    if (node.Attributes["tts:fontSize"] != null)
                        fontSize = node.Attributes["tts:fontSize"].InnerText;


                    textBoxStyleName.Text = name;
                    comboBoxFontName.Text = fontFamily;

                    textBoxFontSize.Text = fontSize;

                    // normal | italic | oblique
                    comboBoxFontStyle.SelectedIndex = 0;
                    if (fontStyle.ToLower() == "italic")
                        comboBoxFontStyle.SelectedIndex = 1;
                    if (fontStyle.ToLower() == "oblique")
                        comboBoxFontStyle.SelectedIndex = 2;

                    // normal | bold
                    comboBoxFontWeight.SelectedIndex = 0;
                    if (fontStyle.ToLower() == "bold")
                        comboBoxFontStyle.SelectedIndex = 1;

                    Color color = Color.White;
                    try
                    {
                        if (fontColor.StartsWith("rgb("))
                        {
                            string[] arr = fontColor.Remove(0, 4).TrimEnd(')').Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            color = Color.FromArgb(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                        }
                        else
                        {
                            color = System.Drawing.ColorTranslator.FromHtml(fontColor);
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show("Unable to read color: " + fontColor + " - " + exception.Message);
                    }
                    panelFontColor.BackColor = color;
                }
            }
        }

        private void GeneratePreview()
        {
            if (_previewTimer.Enabled)
            {
                _previewTimer.Stop();
                _previewTimer.Start();
            }
            else
            {
                _previewTimer.Start();
            }
        }

        private void buttonFontColor_Click(object sender, EventArgs e)
        {
            colorDialogStyle.Color = panelFontColor.BackColor;
            if (colorDialogStyle.ShowDialog() == DialogResult.OK)
            {
                listViewStyles.SelectedItems[0].SubItems[3].BackColor = colorDialogStyle.Color;
                listViewStyles.SelectedItems[0].SubItems[3].Text = Utilities.ColorToHex(colorDialogStyle.Color);
                panelFontColor.BackColor = colorDialogStyle.Color;
                UpdateHeaderXml(listViewStyles.SelectedItems[0].Text, "tts:color", Utilities.ColorToHex(colorDialogStyle.Color));
                GeneratePreview();
            }
        }

        private void buttonRemoveAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewStyles.Items)
            {
                UpdateHeaderXmlRemoveStyle(item.Text);
            }
            listViewStyles.Items.Clear();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (listViewStyles.SelectedItems.Count == 1)
            {
                int index = listViewStyles.SelectedItems[0].Index;
                string name = listViewStyles.SelectedItems[0].Text;

                UpdateHeaderXmlRemoveStyle(listViewStyles.SelectedItems[0].Text);
                listViewStyles.Items.RemoveAt(listViewStyles.SelectedItems[0].Index);

                if (index >= listViewStyles.Items.Count)
                    index--;
                listViewStyles.Items[index].Selected = true;
            }
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string name = "new";
            int count = 2;
            while (StyleExists(name))
            {
                name = "new" + count;
                count++;
            }
            AddStyle(name, "Arial", "white", "100%");
            AddStyleToXml(name, "Arial", "normal", "normal", "white", "100%");
            listViewStyles.Items[listViewStyles.Items.Count - 1].Selected = true;
        }

        private void AddStyleToXml(string name, string fontFamily, string fontWeight, string fontStyle, string color, string fontSize)
        {
            TimedText10.AddStyleToXml(_xml, _xmlHead, _nsmgr,name, fontFamily, fontWeight, fontStyle, color, fontSize);
        }

        private bool StyleExists(string name)
        {
            foreach (ListViewItem item in listViewStyles.Items)
            {
                if (item.Text == name)
                    return true;
            }
            return false;
        }

        private void textBoxStyleName_TextChanged(object sender, EventArgs e)
        {
            if (listViewStyles.SelectedItems.Count == 1)
            {
                if (_doUpdate)
                {
                    if (!StyleExists(textBoxStyleName.Text))
                    {
                        UpdateHeaderXml(listViewStyles.SelectedItems[0].Text, "xml:id", textBoxStyleName.Text);
                        textBoxStyleName.BackColor = listViewStyles.BackColor;
                        listViewStyles.SelectedItems[0].Text = textBoxStyleName.Text;
                    }
                    else
                    {
                        textBoxStyleName.BackColor = Color.LightPink;
                    }
                }
            }
        }

        private void UpdateHeaderXml(string id, string tag, string value)
        {
            foreach (XmlNode innerNode in _xmlHead)
            {
                if (innerNode.Name == "styling")
                {
                    foreach (XmlNode innerInnerNode in innerNode)
                    {
                        if (innerInnerNode.Name == "style")
                        {
                            XmlAttribute idAttr = innerInnerNode.Attributes["xml:id"];
                            if (idAttr != null && idAttr.InnerText == id)
                            {
                                XmlAttribute attr = innerInnerNode.Attributes[tag];
                                if (attr == null)
                                {
                                    attr = _xml.CreateAttribute("tts:fontSize", "http://www.w3.org/ns/10/ttml#style");
                                    innerInnerNode.Attributes.Append(attr);
                                }
                                attr.InnerText = value;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateHeaderXmlRemoveStyle(string id)
        {
            foreach (XmlNode innerNode in _xmlHead)
            {
                if (innerNode.Name == "styling")
                {
                    foreach (XmlNode innerInnerNode in innerNode)
                    {
                        if (innerInnerNode.Name == "style")
                        {
                            XmlAttribute idAttr = innerInnerNode.Attributes["xml:id"];
                            if (idAttr != null && idAttr.InnerText == id)
                            {
                                innerNode.RemoveChild(innerInnerNode);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void comboBoxFontName_TextChanged(object sender, EventArgs e)
        {
            if (listViewStyles.SelectedItems.Count == 1 && _doUpdate)
            {
                listViewStyles.SelectedItems[0].SubItems[1].Text = comboBoxFontName.Text;
                UpdateHeaderXml(listViewStyles.SelectedItems[0].Text, "tts:fontFamily", comboBoxFontName.Text);
                GeneratePreview();
            }
        }

        private void textBoxFontSize_TextChanged(object sender, EventArgs e)
        {
            if (listViewStyles.SelectedItems.Count == 1 && _doUpdate)
            {
                listViewStyles.SelectedItems[0].SubItems[2].Text = textBoxFontSize.Text;
                UpdateHeaderXml(listViewStyles.SelectedItems[0].Text, "tts:fontSize", textBoxFontSize.Text);
                GeneratePreview();
            }
        }

        private void comboBoxFontStyle_TextChanged(object sender, EventArgs e)
        {
            if (listViewStyles.SelectedItems.Count == 1 && _doUpdate)
            {
                UpdateHeaderXml(listViewStyles.SelectedItems[0].Text, "tts:fontStyle", comboBoxFontStyle.Text);
                GeneratePreview();
            }
        }

        private void comboBoxFontWeight_TextChanged(object sender, EventArgs e)
        {
            if (listViewStyles.SelectedItems.Count == 1 && _doUpdate)
            {
                UpdateHeaderXml(listViewStyles.SelectedItems[0].Text, "tts:fontWeight", comboBoxFontWeight.Text);
                GeneratePreview();
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void TimedTextStyles_ResizeEnd(object sender, EventArgs e)
        {
            GeneratePreview();
        }

        private void TimedTextStyles_SizeChanged(object sender, EventArgs e)
        {
            GeneratePreview();
        }

    }
}
