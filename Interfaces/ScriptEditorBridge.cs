using System;
using System.Windows.Forms;

namespace ScriptingForm.Interfaces
{
    public class ScriptEditorBridge
    {
        private static Editor _editorInstance;
        private static readonly object _lock = new object();

        public static void ShowEditor(IWin32Window owner)
        {
            if (owner is Form ownerForm)
            {
                ownerForm.Invoke((MethodInvoker)delegate {
                    lock (_lock)
                    {
                        if (_editorInstance == null || _editorInstance.IsDisposed)
                        {
                            _editorInstance = new Editor();
                        }

                        if (!_editorInstance.Visible)
                        {
                            _editorInstance.Show(ownerForm);
                        }
                        else
                        {
                            _editorInstance.BringToFront();
                        }
                    }
                });
            }
        }
    }
}