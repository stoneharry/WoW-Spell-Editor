using MySql.Data.MySqlClient;
using Renci.SshNet;
using SpellEditor.Sources.Binding;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SpellEditor.Sources.Database
{
    public class SSHMySQL : MySQL
    {
        protected SshClient sshClient;

        protected readonly string sshHost;
        protected readonly string sshUser;
        protected readonly int sshPort;

        public SSHMySQL(string sshHost, string sshUser, int sshPort)
        {
            this.sshHost = sshHost;
            this.sshUser = sshUser;
            this.sshPort = sshPort;

            DelayedCreateConnection();
        }

        protected override void CreateConnection()
        {
        }

        protected void DelayedCreateConnection()
        {
            ConnectionInfo cnnInfo;
            using (var stream = new FileStream("private-ssh-key.pem", FileMode.Open, FileAccess.Read))
            {
                var file = new PrivateKeyFile(stream);
                var authMethod = new PrivateKeyAuthenticationMethod(sshUser, file);
                cnnInfo = new ConnectionInfo(sshHost, sshPort, sshUser, authMethod);
            }

            sshClient = new SshClient(cnnInfo);
            sshClient.Connect();
            if (sshClient.IsConnected)
            {
                var forwardedPort = new ForwardedPortLocal("127.0.0.1", Config.Config.Host, uint.Parse(Config.Config.Port));
                sshClient.AddForwardedPort(forwardedPort);
                forwardedPort.Start();

                string connStr = $"server={forwardedPort.BoundHost};port={forwardedPort.BoundPort};uid={Config.Config.User};pwd={Config.Config.Pass};Charset=utf8;";

                _connection = new MySqlConnection(connStr);
                _connection.Open();
            }
            else
                throw new Exception("SSH Client failed to connect");

            // Create DB if not exists and use
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`; USE `{0}`;", Config.Config.Database);
                cmd.ExecuteNonQuery();
            }
            // Rather than attempting to recreate the connection on being dropped,
            //  instead just have a keep alive heartbeat.
            // Object reference needs to be held to prevent garbage collection.
            _heartbeat = CreateKeepAliveTimer(TimeSpan.FromMinutes(2));
        }

        ~SSHMySQL()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
            sshClient?.Disconnect();
            sshClient?.Dispose();
            _heartbeat?.Dispose();
            _heartbeat = null;
        }
    }
}
