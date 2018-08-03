using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace hanSqlLib {
	public class hanMysqlHelper: IDisposable {
		private MySqlConnection m_conn = null;

		public hanMysqlHelper () {
			m_conn = new MySqlConnection ($"Server=127.0.0.1;Database=db_test;Port=3306;User=root;Password=123456;CharSet=utf8mb4;Connection Timeout=600;SslMode=none");
			m_conn.Open ();
		}

		~hanMysqlHelper () {
			Dispose ();
		}

		public void Dispose () {
			if (m_conn != null && (m_conn.State == ConnectionState.Broken || m_conn.State == ConnectionState.Open))
				m_conn.Close ();
			m_conn = null;
			GC.SuppressFinalize (this);
		}

		private T _execute_yield<T> (string str_cmd, object[] values, Func<MySqlCommand, T> f) {
			var keys = (from p in Regex.Matches (str_cmd, @"@[0-9|a-z|A-Z|_]+") select p.Value).Distinct ().ToArray ();
			if (keys.Length != values.Length)
				throw new ArgumentException ("数据库查询参数格式不匹配");
			using (var cmd = new MySqlCommand (str_cmd, m_conn)) {
				for (int i = 0; i < keys.Length - 1; i += 2)
					cmd.Parameters.Add (new MySqlParameter (keys[i], values[i]));
				return f (cmd);
			}
		}

		private Task<T> _execute_yield_async<T> (string str_cmd, object [] values, Func<MySqlCommand, Task<T>> f) {
			var keys = (from p in Regex.Matches (str_cmd, @"@[0-9|a-z|A-Z|_]+") select p.Value).Distinct ().ToArray ();
			if (keys.Length != values.Length)
				throw new ArgumentException ("数据库查询参数格式不匹配");
			using (var cmd = new MySqlCommand (str_cmd, m_conn)) {
				for (int i = 0; i < keys.Length - 1; i += 2)
					cmd.Parameters.Add (new MySqlParameter (keys [i], values [i]));
				return f (cmd);
			}
		}

		public string execute_scalar (string str_cmd, params object[] values) {
			return _execute_yield (str_cmd, values, (cmd) => {
				return cmd.ExecuteScalar ().to_str ();
			});
		}

		public string[] execute_multi_scalar (string str_cmd, params object[] values) {
			return _execute_yield (str_cmd, values, (cmd) => {
				DataSet ds = new DataSet ();
				MySqlDataAdapter sda = new MySqlDataAdapter (cmd);
				sda.Fill (ds);
				return (from p in get_items (ds.Tables) select p.Rows[0][0].to_str ()).ToArray ();
			});
		}

		public DataTable execute_table (string str_cmd, params object[] values) {
			return _execute_yield (str_cmd, values, (cmd) => {
				DataSet ds = new DataSet ();
				MySqlDataAdapter sda = new MySqlDataAdapter (cmd);
				sda.Fill (ds);
				return ds.Tables[0];
			});
		}

		public int execute_nonquery (string str_cmd, params object[] values) {
			return _execute_yield (str_cmd, values, (cmd) => {
				return cmd.ExecuteNonQuery ();
			});
		}

		public string[] execute_item (string str_cmd, params object[] values) {
			DataTable dt = execute_table (str_cmd, values);
			if (dt.Rows.Count < 1)
				return new string[0];
			return (from p in dt.Rows[0].ItemArray select p.to_str ()).ToArray ();
		}

		public string[] execute_list (string str_cmd, params object[] values) {
			DataTable dt = execute_table (str_cmd, values);
			return (from p in get_items (dt.Rows) select p[0].to_str ()).ToArray ();
		}

		public string[,] execute_array (string str_cmd, params object[] values) {
			DataTable dt = execute_table (str_cmd, values);
			var ret = new string [dt.Rows.Count, dt.Columns.Count];
			for (int i = 0; i < ret.GetLength (0); ++i) {
				for (int j = 0; j < ret.GetLength (1); ++j)
					ret [i, j] = dt.Rows [i] [j].to_str ();
			}
			return ret;
		}

		public string[][] execute_array2 (string str_cmd, params object[] values) {
			DataTable dt = execute_table (str_cmd, values);
			return (from p in get_items (dt.Rows) select (from q in p.ItemArray select q.to_str ()).ToArray ()).ToArray ();
		}

		public async Task<string> execute_scalar_async (string str_cmd, params object [] values) {
			return await _execute_yield_async (str_cmd, values, async (cmd) => {
				return (await cmd.ExecuteScalarAsync ()).to_str ();
			});
		}

		public async Task<DataTable> execute_table_async (string str_cmd, params object [] values) {
			return await _execute_yield_async (str_cmd, values, async (cmd) => {
				DataSet ds = new DataSet ();
				MySqlDataAdapter sda = new MySqlDataAdapter (cmd);
				await sda.FillAsync (ds);
				return ds.Tables [0];
			});
		}

		public async Task<int> execute_nonquery_async (string str_cmd, params object [] values) {
			return await _execute_yield_async (str_cmd, values, async (cmd) => {
				return await cmd.ExecuteNonQueryAsync ();
			});
		}

		public async Task<string[]> execute_item_async (string str_cmd, params object [] values) {
			DataTable dt = await execute_table_async (str_cmd, values);
			if (dt.Rows.Count < 1)
				return new string [0];
			return (from p in dt.Rows [0].ItemArray select p.to_str ()).ToArray ();
		}

		public async Task<string[]> execute_list_async (string str_cmd, params object [] values) {
			DataTable dt = await execute_table_async (str_cmd, values);
			return (from p in get_items (dt.Rows) select p [0].to_str ()).ToArray ();
		}

		public async Task<string[,]> execute_array_async (string str_cmd, params object [] values) {
			DataTable dt = await execute_table_async (str_cmd, values);
			var ret = new string [dt.Rows.Count, dt.Columns.Count];
			for (int i = 0; i < ret.GetLength (0); ++i) {
				for (int j = 0; j < ret.GetLength (1); ++j)
					ret [i, j] = dt.Rows [i] [j].to_str ();
			}
			return ret;
		}

		public async Task<string[][]> execute_array2_async (string str_cmd, params object [] values) {
			DataTable dt = await execute_table_async (str_cmd, values);
			return (from p in get_items (dt.Rows) select (from q in p.ItemArray select q.to_str ()).ToArray ()).ToArray ();
		}

		private static DataTable[] get_items (DataTableCollection dtc) {
			if (dtc == null)
				return new DataTable[0];
			DataTable[] ret = new DataTable[dtc.Count];
			for (int i = 0; i < dtc.Count; ++i)
				ret[i] = dtc[i];
			return ret;
		}

		private static DataRow[] get_items (DataRowCollection drc) {
			if (drc == null)
				return new DataRow[0];
			DataRow[] ret = new DataRow[drc.Count];
			for (int i = 0; i < drc.Count; ++i)
				ret[i] = drc[i];
			return ret;
		}
	}
}
