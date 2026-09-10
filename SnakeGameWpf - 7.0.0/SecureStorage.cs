using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SnakeGame
{
    /// <summary>
    /// AES-256 加密封装。所有配置读写都通过本类。
    /// 文件格式：[magic "SG01"(4B)][IV(16B)][ciphertext]
    /// 兼容旧明文文件：读取时若发现无 magic，则按明文解析。
    /// </summary>
    public static class SecureStorage
    {
        private static readonly byte[] Magic = { (byte)'S', (byte)'G', (byte)'0', (byte)'1' };

        // ★ AES-256 密钥（32 字节）。若想换一个自己的密钥，直接替换这 32 个字节。
        //    注意：硬编码密钥只能阻止"普通用户直接改文件"，无法阻止反编译。
        private static readonly byte[] Key = new byte[32]
        {
            0x9C, 0x1A, 0x5E, 0xB2, 0x37, 0x0D, 0x8F, 0x41,
            0x2C, 0xD7, 0x63, 0x58, 0xAE, 0x11, 0xF4, 0x92,
            0x4B, 0xE6, 0x0A, 0x1D, 0x7C, 0x35, 0x90, 0xBF,
            0x28, 0x6F, 0xC4, 0x51, 0x03, 0x9E, 0x8B, 0x76
        };

        // ---------- 保存 ----------
        public static void SaveText(string path, string text)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                byte[] plain = Encoding.UTF8.GetBytes(text ?? "");

                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.GenerateIV();

                    using (var enc = aes.CreateEncryptor())
                    {
                        byte[] cipher = enc.TransformFinalBlock(plain, 0, plain.Length);

                        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            fs.Write(Magic, 0, Magic.Length);
                            fs.Write(aes.IV, 0, aes.IV.Length);
                            fs.Write(cipher, 0, cipher.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SecureStorage.SaveText 失败: " + ex.Message);
            }
        }

        // ---------- 读取 ----------
        public static string LoadText(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                byte[] all = File.ReadAllBytes(path);
                if (all.Length == 0) return null;

                // 判断是否有 magic
                bool encrypted = all.Length >= Magic.Length;
                if (encrypted)
                {
                    for (int i = 0; i < Magic.Length; i++)
                    {
                        if (all[i] != Magic[i]) { encrypted = false; break; }
                    }
                }

                // —— 旧明文文件：直接返回，并提示（下次保存会自动加密）——
                if (!encrypted)
                    return Encoding.UTF8.GetString(all);

                // —— 加密文件：解密 ——
                if (all.Length < Magic.Length + 16) return null;

                byte[] iv = new byte[16];
                Array.Copy(all, Magic.Length, iv, 0, 16);

                int cipherLen = all.Length - Magic.Length - 16;
                byte[] cipher = new byte[cipherLen];
                Array.Copy(all, Magic.Length + 16, cipher, 0, cipherLen);

                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = iv;
                    using (var dec = aes.CreateDecryptor())
                    {
                        byte[] plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                        return Encoding.UTF8.GetString(plain);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SecureStorage.LoadText 失败: " + ex.Message);
                return null;
            }
        }

        /// <summary>删除配置文件（可选工具方法）</summary>
        public static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}