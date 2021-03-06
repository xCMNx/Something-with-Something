﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.BaseClasses;
using core.Interfaces;
using core.Interfaces.Tracker;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;

namespace tracker_redmine
{
	public class Redmine : BindableBase, ITracker, IModuleConfigurable
	{
		protected const string PREFIX = "REDMINE_";
		protected const string USER = PREFIX + "USER[{0}]";
		protected const string PASS = PREFIX + "PASS[{0}]";
		protected const string URL = PREFIX + "URL[{0}]";
		protected const string TRACKERS_FILTER = PREFIX + "TRACKERS_FILTER[{0}]";
		protected const string STATUS_FILTER = PREFIX + "STATUS_FILTER[{0}]";

		public string ConfigName { get; set; }

		public string Url
		{
			get { return Helpers.ConfigRead(string.Format(URL, ConfigName), string.Empty, true); }
			set { Helpers.ConfigWrite(string.Format(URL, ConfigName), value); }
		}

		public string User
		{
			get { return Helpers.ConfigRead(string.Format(USER, ConfigName), string.Empty, true); }
			set { Helpers.ConfigWrite(string.Format(USER, ConfigName), value); }
		}

		public string Password
		{
			get { return Helpers.ConfigRead(string.Format(PASS, ConfigName), string.Empty, true); }
			set { Helpers.ConfigWrite(string.Format(PASS, ConfigName), value); }
		}

		public string Trackers
		{
			get { return Helpers.ConfigRead(string.Format(TRACKERS_FILTER, ConfigName), string.Empty, true); }
			set { Helpers.ConfigWrite(string.Format(TRACKERS_FILTER, ConfigName), value); }
		}

		public string Statuses
		{
			get { return Helpers.ConfigRead(string.Format(STATUS_FILTER, ConfigName), string.Empty, true); }
			set { Helpers.ConfigWrite(string.Format(STATUS_FILTER, ConfigName), value); }
		}

		ObservableCollectionEx<IProject> _Projects = new ObservableCollectionEx<IProject>();
		public IList<IProject> Projects => _Projects;

		public string Name => "Redmine";

		protected async Task<bool> AskAuthInfo(ParametersRequest parametersRequest, string message = null)
		{
			var dict = new IParametersRequestItem[] {
				new ParametersRequestItem(){ Title = "Url", Value = new StringValueItem(Url) }
				,new HeaderRequestItem() { Title = "Авторизация" }
				,new ParametersRequestItem(){ Title = "User", Value = new StringValueItem(User) }
				,new ParametersRequestItem(){ Title = "Password", Value = new PasswordValueItem(Password) }
			};

			if (await parametersRequest(dict, "Redmine: Настройки соединения", message))
			{
				Url = (dict[0].Value as StringValueItem).String;
				User = (dict[2].Value as StringValueItem).String;
				Password = (dict[3].Value as StringValueItem).String;
				return true;
			}
			return false;
		}

		protected RedmineManager Manager;
		protected async Task<RedmineManager> GetNew(ParametersRequest parametersRequest)
		{
			string message = null;
			while (await AskAuthInfo(parametersRequest, message))
			{
				try
				{
					var manager = new RedmineManager(Url, User, Password);
					manager.GetCurrentUser();
					return manager;
				}
				catch (Exception e)
				{
					message = e.Message;
					Helpers.ConsoleWrite(message, ConsoleColor.Red);
				}
			}
			return null;
		}

		public void UpdateProjects(RedmineManager manager)
		{
			var projects = manager.GetObjects<Project>(new NameValueCollection());
			Helpers.Send(() => _Projects.Reset(projects.Select(p => new RedmineProject() { Title = p.Name, Identifier = p.Id })));
		}

		bool reconfig(RedmineManager manager)
		{
			if (manager != null)
			{
				UpdateProjects(manager);
				Manager = manager;
				return true;
			}
			return false;
		}

		public async Task<bool> UpdateSettingsAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			try
			{
				return reconfig(await GetNew(parametersRequest));
			}
			catch (Exception e)
			{
				await showText(e.Message);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return false;
		}

		public async Task<bool> UpdateAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			try
			{
				RedmineManager manager = null;
				try
				{
					manager = new RedmineManager(Url, User, Password);
					manager.GetCurrentUser();
				}
				catch
				{
					manager = await GetNew(parametersRequest);
				}
				return reconfig(manager);
			}
			catch (Exception e)
			{
				await showText(e.Message);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return false;
		}

		readonly char[] ValuesDelimiter = { '|' };

		public async Task<bool> ConfigurateAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			try
			{
				var trackers = Trackers.Split(ValuesDelimiter, StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i)).ToArray();
				var statuses = Statuses.Split(ValuesDelimiter, StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i)).ToArray();
				var trks = Manager.GetObjects<Tracker>(new NameValueCollection()).ToDictionary(t => t.Name);
				var stts = Manager.GetObjects<IssueStatus>(new NameValueCollection()).ToDictionary(s => s.Name);
				var tItems = trks.Select(t => new CheckValueItem(trackers.Contains(t.Value.Id), t.Value.Name)).ToArray();
				var sItems = stts.Select(s => new CheckValueItem(statuses.Contains(s.Value.Id), s.Value.Name)).ToArray();

				var dict = new ParametersRequestItem[] {
					new ParametersRequestItem(){
						Title = "Трекеры",
						Value = new ListValueItem(tItems)
					}
					,new ParametersRequestItem(){
						Title = "Статусы",
						Value = new ListValueItem(sItems)
					}
				};

				if (await parametersRequest(dict, "Redmine: Настройки фильтра"))
				{
					Trackers = string.Join("|", tItems.Where(itm => itm.IsChecked).Select(itm => trks[itm.Title].Id.ToString()).ToArray());
					Statuses = string.Join("|", sItems.Where(itm => itm.IsChecked).Select(itm => stts[itm.Title].Id.ToString()).ToArray());
					return true;
				}
			}
			catch (Exception e)
			{
				await showText(e.Message);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return false;
		}

		public IList<IIssue> GetIssues(IProject project, ShowText showText)
		{
			try
			{
				var query = new NameValueCollection() { { "assigned_to_id", "me" } };
				if (project != null)
					query.Add("project_id", project.Identifier.ToString());
				var trackers = Trackers;
				if (!string.IsNullOrWhiteSpace(trackers))
					query.Add("tracker_id", trackers);
				var stats = Statuses;
				if (!string.IsNullOrWhiteSpace(stats))
					query.Add("status_id", stats);
				IList<Issue> issues = Manager.GetObjects<Issue>(query).OrderByDescending(i => i.Priority.Id).ToArray();

				return issues.Select(i => new RedmineIssue()
				{
					Title = i.Subject,
					Tracker = i.Tracker?.Name,
					Description = string.Format("Приоритет: {6}\r\nСостояние: {0}\r\nАвтор: {1}\r\nСоздана: {2}\r\nОбновлена: {3}\r\nКатегория: {4}\r\n\r\n{5}", i.Status?.Name, i.Author?.Name, i.CreatedOn, i.UpdatedOn, i.Category?.Name, i.Description, i.Priority.Name),
					Identifier = i.Id,
					Priority = i.Priority.Id
				}).ToArray();
			}
			catch (Exception e)
			{
				showText(e.Message);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return new IIssue[0];
		}

		public Task<IList<IIssue>> GetIssuesAsync(IProject project, ShowText showText)
		{
			return Task<IList<IIssue>>.Factory.StartNew(() => GetIssues(project, showText));
		}
	}
}
