using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using core;
using Ui;

namespace SwS
{
	/// <summary>
	/// Логическая обертка для контрола Question
	/// </summary>
	public class QuestionBlock : BindableBase
	{
		public ObservableCollectionEx<object> Fields { get; protected set; } = new ObservableCollectionEx<object>();
		public ObservableCollectionEx<string> Messages { get; protected set; } = new ObservableCollectionEx<string>();
		public string Title { get; protected set; }
		public Visibility Visibility { get; protected set; }

		/// <summary>
		/// Подготавливает данные для Question
		/// </summary>
		void BuildFrom(IList<IParametersRequestItem> requestFields, string title = null, params string[] messages)
		{
			Fields.Reset(requestFields);
			//Отсеим пустые строки
			Messages.Reset(messages.Where(m => !string.IsNullOrWhiteSpace(m)));
			Title = title;
			NotifyPropertyChanged(nameof(Title));
		}

		void setVisibility(Visibility vis)
		{
			Visibility = vis;
			NotifyPropertyChanged(nameof(Visibility));
		}

		/// <summary>
		/// Устанавливает результат работы Question, прячет его, и устанавивает сигнал для вызвающего потока
		/// </summary>
		void SetResult(bool res)
		{
			result = res;
			setVisibility(Visibility.Hidden);
			_ready.Set();
		}

		public Command Ok { get; private set; }
		public Command Cancel { get; private set; }

		public QuestionBlock()
		{
			Ok = new Command((p) => SetResult(true));
			Cancel = new Command((p) => SetResult(false));
		}

		void innerShow(IList<IParametersRequestItem> requestFields, string title = null, params string[] messages)
		{
			BuildFrom(requestFields, title, messages);
			setVisibility(Visibility.Visible);
		}

		/// <summary>
		/// Для блокировки вызывающего потока и его восстановления после выполнения одной из команд
		/// </summary>
		ManualResetEvent _ready = new ManualResetEvent(false);
		/// <summary>
		/// Для очереди вызова Question т.к. два запроса не могут одновременно использовать один и тотже контрол,
		/// иначе получится каша.
		/// </summary>
		SemaphoreSlim ShowLock = new SemaphoreSlim(1);
		/// <summary>
		/// Через него передадим результат
		/// </summary>
		bool result;

		///Метод должен вызываться из потока, т.к. вызывающий поток будет заблокирован, и освобожден только коммандами.
		///Если наш поток окажется основным, то произойдет дэдлок.
		protected bool Show(IList<IParametersRequestItem> requestFields, string title = null, params string[] messages)
		{
			//залочим поток семафором если ктото уже вызвал Question
			ShowLock.Wait();
			try
			{
				//сбросим сигнал
				_ready.Reset();
				//передадим работу в основной поток
				Helpers.Post(() => innerShow(requestFields, title, messages));
				//ждем сигнал, оный придет после отработки одной из комманд
				_ready.WaitOne();
				return result;
			}
			finally
			{
				ShowLock.Release();
			}
		}

		public Task<bool> ShowAsync(IList<IParametersRequestItem> requestFields, string title = null, params string[] messages)
		{
			return Task<bool>.Factory.StartNew(() => Show(requestFields, title, messages));
		}
	}
}
