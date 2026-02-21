using Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static Client.Services.ProjectService;

namespace Client.Services
{
    public class ProjectFlossState
    {
        private List<FlossInProjectModel>? _floss;
        public IReadOnlyList<FlossInProjectModel>? Floss => _floss is null ? null : _floss.OrderBy(f => f.Id).ToList();
        public int? LoadedForProjectId { get; private set; }

        public bool IsLoading { get; private set; }
        public string? LastError { get; private set; }
        public event Action? Changed;


        public int? SelectedFlossId { get; private set; }
        public FlossInProjectModel? Selected => SelectedFlossId is int id ?
            _floss.FirstOrDefault(f => f.Id == id) : null;
        public FlossInProjectModel? Draft { get; private set; }

        public Dictionary<int, SymbolData>? SymbolDictionary { get; private set; }
        public bool? HasNullFloss => SymbolDictionary is null ? null : SymbolDictionary.Any(x => x.Value.Floss is null);
        public int DefaultStrand { get; private set; }
        public List<FlossInProjectModel>? FlossToBuy { get; private set; }

        public void BeginLoad()
        {
            IsLoading = true;
            LastError = null;
            Changed?.Invoke();
        }

        public void SetError(string error)
        {
            LastError = error;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void Clear()
        {
            _floss.Clear();
            LoadedForProjectId = null;
            IsLoading = false;
            LastError = null;
            SelectedFlossId = null;
            SymbolDictionary = null;
            Draft = null;
            Changed?.Invoke();
        }

        private static FlossInProjectModel Clone(FlossInProjectModel f) => new()
        {
            Id = f.Id,
            Name = f.Name,
            Number = f.Number,
            HexColor = f.HexColor,
            Amount = f.Amount,
            Strands = f.Strands
        };

        private void Upsert(FlossInProjectModel floss)
        {
            var idx = _floss.FindIndex(f => f.Id == floss.Id);
            if (idx >= 0) _floss[idx] = floss;
            else _floss.Add(floss);
        }

        public void SetFlossList(int projectId, IEnumerable<FlossInProjectModel>? floss, bool dontsettonullifzero = false)
        {
            LoadedForProjectId = projectId;
            _floss = floss?.ToList() ?? null;
            if (_floss is not null && _floss.Count == 0 && !dontsettonullifzero) _floss = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetSelectedId(int? id)
        {
            SelectedFlossId = id;
            LastError = null;
            Changed?.Invoke();
        }

        public void SetSymbolDictionary(Dictionary<int, SymbolData>? symbolDictionary)
        {
            SymbolDictionary = symbolDictionary;
            Changed?.Invoke();
        }

        public void UpdateSymbolDictionary(Dictionary<int, int> SymbolCount)
        {
            foreach(var item in SymbolCount)
            {
                var symbol = item.Key;
                var count = item.Value;

                if (SymbolDictionary.TryGetValue(symbol, out var sd))
                    sd.Count = count;
            }
            Changed?.Invoke();
        }

        public void PopulateBee(Dictionary<int, SymbolData> beeDictionary)
        {
            foreach(var entry in beeDictionary)
            {
                if(SymbolDictionary.TryGetValue(entry.Key, out var sd))
                {
                    sd.Floss = entry.Value.Floss;
                }
            }
            Changed?.Invoke();
        }

        public void DeNullSymbolDictionaryEntry((int, int) entry)
        {
            int key = entry.Item1;
            int flossId = entry.Item2;
            SymbolDictionary[key].Floss = new(flossId);
        }

        public void SetStrand(int x)
        {
            DefaultStrand = x;
            Changed?.Invoke();
        }

        public void SetFlossToBuy(List<FlossInProjectModel>? flosses)
        {
            FlossToBuy = flosses;
            Changed?.Invoke();
        }

        public bool BeginEdit(int id)
        {
            IsLoading = true;

            var current = _floss.FirstOrDefault(f => f.Id == id);
            if (current is null) return false;

            Draft = Clone(current);
            LastError = null;

            Changed?.Invoke();
            return true;
        }
        public void CancelEdit()
        {
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
        public void ApplyEdit(FlossInProjectModel floss)
        {
            Upsert(floss);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void BeginCreate()
        {
            IsLoading = true;
            Draft = new()
            {
                Id = 0,
                Name = null,
                Number = null,
                HexColor = null,
                Amount = 0,
                Strands = DefaultStrand
            };
            LastError = null;
            Changed?.Invoke();
        }

        public void CancelCreate()
        {
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
        public void ApplyCreate(FlossInProjectModel floss)
        {
            Upsert(floss);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public bool BeginDelete(int id)
        {
            IsLoading = true;

            var current = _floss.FirstOrDefault(f => f.Id == id);
            if (current is null) return false;

            LastError = null;
            Changed?.Invoke();
            return true;
        }

        public void CancelDelete()
        {
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void ApplyDelete(int id)
        {
            _floss.RemoveAll(f => f.Id == id);
            Draft = null;
            SelectedFlossId = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void BeginReset()
        {
            SelectedFlossId = null;
            LastError = null;
            Changed?.Invoke();
        }

        public void CancelReset()
        {
            LastError = null;
            Changed?.Invoke();
        }

        public void ApplyReset()
        {
            _floss = null;
            LastError = null;
            Changed?.Invoke();
        }
    }
    public class ProjectFlossService
    {
        private readonly ProjectFlossState _state;
        private readonly HttpClient _http;
        public ProjectFlossService(ProjectFlossState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }

        public async Task<Result> LoadForProjectAsync(int projectId, bool forceReload = false)
        {
            _state.BeginLoad();

            if (!forceReload && _state.Floss is not null && _state.LoadedForProjectId == projectId) return Result.Success();

            var response = await _http.GetAsync($"api/projects/{projectId}/floss");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var floss = await response.Content.ReadFromJsonAsync<List<FlossInProjectModel>?>();
            _state.SetFlossList(projectId, floss);
            return Result.Success();
        }

        public bool BeginEdit(int id) => _state.BeginEdit(id);
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> SaveEditAsync()
        {
            var draft = _state.Draft;
            if (draft is null)
            {
                _state.SetError("Nothing to save");
                return Result.Fail();
            }

            var projectId = _state.LoadedForProjectId;
            var flossId = _state.SelectedFlossId;

            var response = await _http.PutAsync($"api/projects/{projectId}/floss/{flossId}/{draft.Amount}/{draft.Strands}", null);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyEdit(draft);
            return Result.Success();
        }

        public void BeginCreate() => _state.BeginCreate();
        public void CancelCreate() => _state.CancelCreate();
        public async Task<Result> SaveCreateAsync()
        {
            var draft = _state.Draft;
            if (draft is null)
            {
                _state.SetError("Nothing to submit.");
                return Result.Fail();
            }
            var projectId = _state.LoadedForProjectId;
            var response = await _http.PostAsync($"api/projects/{projectId}/floss/{draft.Id}/{draft.Amount}/{draft.Strands}", null);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var newFloss = await response.Content.ReadFromJsonAsync<FlossInProjectModel>();
            _state.ApplyCreate(newFloss!);
            return Result.Success();
        }

        public bool BeginDelete(int id) => _state.BeginDelete(id);
        public void CancelDelete() => _state.CancelDelete();
        public async Task<Result> DeleteAsync(int id)
        {
            var projectId = _state.LoadedForProjectId;
            var response = await _http.DeleteAsync($"api/projects/{projectId}/floss/{id}");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyDelete(id);
            return Result.Success();
        }

        public void BeginReset() => _state.BeginReset();
        public void CancelReset() => _state.CancelReset();
        public async Task<Result> ApplyResetAsync()
        {
            var projectId = _state.LoadedForProjectId;
            var response = await _http.DeleteAsync($"api/projects/{projectId}/floss/reset");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyReset();
            return Result.Success();
        }

        public async Task<Result> SaveCalculation(List<FlossInProjectModel>? flosses, bool NoAmountOverride = true)
        {
            var projectId = _state.LoadedForProjectId;
            if(projectId is null) return Result.Fail();

            if(flosses is not null)
            {
                _state.SetFlossList((int)projectId, flosses);
            }

            foreach (var floss in _state.Floss) Console.WriteLine(floss.Amount);
            //Check if any of the amounts are 0
            if (NoAmountOverride)
            {
                bool hasZeroCount = _state.Floss.Any(f => f.Amount == 0);
                if (hasZeroCount) return Result.PartialKey();
            }


            var response = await _http.PostAsJsonAsync<List<FlossInProjectModel>>
                ($"api/projects/{projectId}/save-calculated-floss", _state.Floss.ToList());
            if (!response.IsSuccessStatusCode)
            {

                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var flossesFromDB = await response.Content.ReadFromJsonAsync<List<FlossInProjectModel>>();
            _state.SetFlossList((int)projectId, flossesFromDB);
            _state.SetSymbolDictionary(null);
            return Result.Success();
        }

        public Result StartNoReadCase(int? strand)
        {
            _state.SetStrand((int)strand);
            _state.SetFlossList((int)_state.LoadedForProjectId, new List<FlossInProjectModel>(), true);

            _state.SetError(_state.Floss is null ? "Floss list is null" : _state.Floss.Count.ToString());
            return Result.Success();
        }

        public void SetSelectedId(int? id)
        {
            var current = _state.SelectedFlossId;
            if (current is null || current != id)
            {
                _state.SetSelectedId(id);
            }
            else _state.SetSelectedId(null);
        }

        public async Task CalculateFlossNeeded()
        {
            if(_state.LoadedForProjectId is null)
            {
                _state.SetError("No project id");
                return;
            }
            
            int projectId = (int)_state.LoadedForProjectId;
            var response = await _http.GetAsync($"api/projects/{projectId}/calculate-floss-needed");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return;
            }

            var flossNeeded = await response.Content.ReadFromJsonAsync<List<FlossInProjectModel>>();
            _state.SetFlossToBuy(flossNeeded);
            return;
        }

        private async Task SetErrorFromResponse(HttpResponseMessage response)
        {
            var message = await response.Content.ReadAsStringAsync();
            _state.SetError(message);
        }

        public async Task PopulateBee()
        {
            var response = await _http.GetAsync("api/projects/BeeDictionary");
            var beeDictionary = await response.Content.ReadFromJsonAsync<Dictionary<int, SymbolData>>();
            _state.PopulateBee(beeDictionary);
        }
    }
}
