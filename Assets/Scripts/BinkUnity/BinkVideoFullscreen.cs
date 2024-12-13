using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;

namespace KC_Custom.Bink
{
    public class BinkVideoFullscreen : MonoBehaviour
    {
        [SerializeField] private string m_filename = "";
        [SerializeField] private bool m_letterbox = true;
        [SerializeField] private bool m_transparent = false;
        [SerializeField] private bool m_noSound = false;
        [SerializeField] private bool m_loop = false;
        [SerializeField] private bool m_ayOnAwake = true;
        [SerializeField] private bool m_turnSubsOff = false;
        [SerializeField] private GUISkin m_subsSkin;
        [SerializeField, Range(0, 255)] private byte m_volume = 255;
    
        private BinkVideo.SubStr[] m_subs;
        [SerializeField] private BinkVideo.TBink m_binkS;
        private XmlDocument m_xDoc = new XmlDocument();
        private Texture2D m_tex, m_blackTex;
        private IntPtr m_hBink, m_pointer;
        private int m_size, m_n;
        private byte[] m_bits;
        private bool m_isPlaying, m_blackTexCreated, m_hasSubs = false;
        private bool m_isStopped = true;
        private float m_timer = 0.0f;
        private uint m_pgoto = 0;
        private bool m_isgoto = false;

        void Start()
        {
            if (this.m_ayOnAwake)
                this.BinkPlay();
        }

        public void BinkPlay()
        {
            BinkVideo.BinkSetSoundSystem(BinkVideo.BinkOpenDirectSound(), IntPtr.Zero);
            this.m_hBink = BinkVideo.BinkOpen(Application.dataPath + "\\" + this.m_filename, BinkVideo.BinkOpenEnum.BINK_OPEN_STREAM);
            if (this.m_noSound)
            {
                this.m_volume = 0;
                BinkVideo.BinkSetVolume(this.m_hBink, 0, 0);
            }
            else BinkVideo.BinkSetVolume(this.m_hBink, 0, this.m_volume * (uint)655.36);
            this.m_binkS = (BinkVideo.TBink)Marshal.PtrToStructure(this.m_hBink, typeof(BinkVideo.TBink));
            if (this.m_transparent)
                this.m_n = 4;
            else this.m_n = 3;
            this.m_size = (int)this.m_binkS.Width * this.m_n * (int)this.m_binkS.Height;
            this.m_bits = new byte[this.m_size];
            this.m_pointer = Marshal.AllocHGlobal(this.m_size);
            if (this.m_transparent)
                this.m_tex = new Texture2D((int)this.m_binkS.Width, (int)this.m_binkS.Height, TextureFormat.ARGB32, false);
            else this.m_tex = new Texture2D((int)this.m_binkS.Width, (int)this.m_binkS.Height, TextureFormat.RGB24, false);
            Vector3 vector = new Vector3(-this.transform.localScale.x, 1, 1);
            this.transform.localScale = vector;
            this.m_isPlaying = true;
            this.m_isStopped = false;
            if (File.Exists(Path.ChangeExtension(Application.dataPath + "\\" + this.m_filename, "xml")))
            {
                if (this.m_subsSkin != null)
                {
                    this.m_xDoc.Load(Path.ChangeExtension(Application.dataPath + "\\" + this.m_filename, "xml"));
                    XmlElement xRoot = this.m_xDoc.DocumentElement;
                    this.m_subs = new BinkVideo.SubStr[xRoot.ChildNodes.Count];
                    int i = 0;
                    this.m_hasSubs = true;
                    foreach (XmlNode xnode in xRoot)
                    {
                        if (xnode.Name == "subtitle")
                        {
                            this.m_subs[i].Start = Convert.ToInt32(xnode.Attributes.GetNamedItem("start").Value);
                            this.m_subs[i].End = Convert.ToInt32(xnode.Attributes.GetNamedItem("end").Value);
                            this.m_subs[i].Text = xnode.ChildNodes[0].InnerText;
                            i++;
                        }
                    }
                }
                else Debug.Log("GUI Skin is not assigned");
            }
            InvokeRepeating("DrawTexture", 0.0f, (1.0f / ((float)this.m_binkS.FrameRate / (float)this.m_binkS.FrameRate2)));
        }

        public bool isBinkStopped()
        {
            if (this.m_isStopped)
                return true;
            return false;
        }

        public void BinkStop()
        {
            this.m_binkS.CurrentFrame = 0;
            BinkVideo.BinkClose(this.m_hBink);
            this.m_isPlaying = false;
            this.m_isStopped = true;
            this.m_timer = 0.0f;
            this.m_hasSubs = false;
            this.CancelInvoke();
        }

        public void BinkGoToFrame(uint a)
        {
            this.m_isgoto = true;
            this.m_pgoto = a;
            BinkVideo.BinkGoto(this.m_hBink, this.m_pgoto - 100, 1);
            BinkVideo.BinkGoto(this.m_hBink, this.m_pgoto, 2);
            this.m_timer = 1.0f / ((float)this.m_binkS.FrameRate / (float)this.m_binkS.FrameRate2) * (float)a;
        }

        void OnGUI()
        {
            if (this.m_isPlaying)
            {
                if (this.m_letterbox)
                {
                    if (!this.m_blackTexCreated)
                    {
                        this.m_blackTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
                        this.m_blackTex.SetPixel(1, 1, Color.black);
                        this.m_blackTex.Apply();
                        this.m_blackTexCreated = true;
                    }
                    float xSc = (float)Screen.width / (float)this.m_binkS.Width;
                    if (!this.m_transparent)
                        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.m_blackTex);
                    GUI.DrawTexture(new Rect(0, Screen.height - ((Screen.height - (this.m_binkS.Height) * xSc) / 2), Screen.width, -this.m_binkS.Height * xSc), this.m_tex);
                }
                else GUI.DrawTexture(new Rect(0, Screen.height, Screen.width, -Screen.height), this.m_tex);
                if ((this.m_hasSubs) && (!this.m_turnSubsOff))
                {
                    for (int i = 0; i < this.m_subs.Length; i++)
                    {
                        if ((this.m_timer >= (float)this.m_subs[i].Start / 1000.0) && (this.m_timer <= (float)this.m_subs[i].End / 1000.0))
                        {
                            this.m_subsSkin.GetStyle("Label").fontSize = Screen.height / 32;
                            this.m_subsSkin.GetStyle("Label").normal.textColor = Color.black;
                            GUI.Label(new Rect(Screen.width / 6 + 1, 1, Screen.width - Screen.width / 6 * 2 + 1, Screen.height + 1), this.m_subs[i].Text, this.m_subsSkin.GetStyle("Label"));
                            this.m_subsSkin.GetStyle("Label").normal.textColor = Color.white;
                            GUI.Label(new Rect(Screen.width/6, 0, Screen.width-Screen.width/6*2, Screen.height), this.m_subs[i].Text, this.m_subsSkin.GetStyle("Label"));
                        }
                    }
                }
            }
        }

        void DrawTexture()
        {
            if ((this.m_binkS.CurrentFrame < this.m_binkS.Frames) || this.m_loop)
            {
                if (!this.m_isgoto)
                {
                    BinkVideo.BinkDoFrame(this.m_hBink);
                    if (this.m_transparent)
                        BinkVideo.BinkCopyToBuffer(this.m_hBink, this.m_pointer, this.m_binkS.Width * (uint)this.m_n, this.m_binkS.Height, 0, 0, BinkVideo.BinkSurface.BINKSURFACE32AR);
                    else BinkVideo.BinkCopyToBuffer(this.m_hBink, this.m_pointer, this.m_binkS.Width * 3, this.m_binkS.Height, 0, 0, BinkVideo.BinkSurface.BINKSURFACE24R);
                    Marshal.Copy(this.m_pointer, this.m_bits, 0, this.m_size);
                    this.m_tex.LoadRawTextureData(m_bits);
                    this.m_tex.Apply();
                    BinkVideo.BinkSetVolume(this.m_hBink, 0, (uint)(this.m_volume * 255));
                    BinkVideo.BinkNextFrame(this.m_hBink);
                    this.m_timer += 1.0f / ((float)this.m_binkS.FrameRate / (float)this.m_binkS.FrameRate2);
                    this.m_binkS.CurrentFrame++;
                }
                else
                {
                    BinkGoToFrame(m_pgoto);
                    this.m_isgoto = false;
                }
            }
            else
            {
                this.m_binkS.CurrentFrame = 0;
                BinkVideo.BinkClose(this.m_hBink);
                this.m_isPlaying = false;
                this.m_isStopped = true;
                this.m_timer = 0.0f;
                this.m_hasSubs = false;
                this.CancelInvoke();
            }
        }
    }
}