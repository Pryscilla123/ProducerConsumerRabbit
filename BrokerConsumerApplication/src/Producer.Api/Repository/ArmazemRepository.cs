using Consumer.Api.Data;
using Consumer.Api.Models;
using Dapper;
using System.Data;

namespace Consumer.Api.Repository
{
    public class ArmazemRepository : IArmazemRepository
    {
        private DbSession _db;

        public ArmazemRepository(DbSession db)
        {
            _db = db;
        }

        public async Task CriarArmazem(Armazem armazem)
        {
            try
            {
                // adicionar o armazem na base de dados
                string query = "INSERT INTO armazem (id, nome, tipo) VALUES (@Id, @Nome, @Tipo)";

                var parameters = new DynamicParameters();
                parameters.Add("Id", armazem.Id, DbType.Guid);
                parameters.Add("Nome", armazem.Nome, DbType.AnsiString);
                parameters.Add("Tipo", armazem.Tipo, DbType.AnsiString);

                var result = await _db.Connection.ExecuteAsync(query, parameters);

                if (result == 0) throw new Exception("Não foi possível inserir o armazém na base de dados");

                query = @"
                    INSERT INTO enderco (armazem_id, rua, bairro, numero, cidade, estado, cep) 
                    VALUES (@ArmazemId, @Rua, @Bairro, @Numero, @Cidade, @Estado, @Cep)
                ";

                parameters.Add("ArmazemId", armazem.Id, DbType.Guid);
                parameters.Add("Rua", armazem.Endereco?.Rua, DbType.AnsiString);
                parameters.Add("Bairro", armazem.Endereco?.Bairro, DbType.AnsiString);
                parameters.Add("Numero", armazem.Endereco?.Numero, DbType.Int16);
                parameters.Add("Cidade", armazem.Endereco?.Cidade, DbType.AnsiString);
                parameters.Add("Estado", armazem.Endereco?.Estado, DbType.AnsiString);
                parameters.Add("Cep", armazem.Endereco?.Cep, DbType.AnsiString);

                result = await _db.Connection.ExecuteAsync(query, parameters);

                if (result == 0) throw new Exception("Não foi possível inserir o endereço do armazém na base de dados");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
