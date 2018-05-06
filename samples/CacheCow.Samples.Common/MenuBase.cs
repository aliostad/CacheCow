using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Samples.Common
{
    public abstract class MenuBase
    {
        public async Task Menu()
        {
            while (true)
            {
                Console.WriteLine(
@"CacheCow Cars Samples - (ASP.NET Core MVC and HttpClient)
    - Press 0 to list all cars
    - Press 1 to create a new car and add to repo
    - Press 2 to update the last item (updates last modified)
    - Press 3 to delete the last item
    - Press 4 to get the last item
    - Press x to exit
"
);
                var key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case 'x':
                        return;
                    case '0':
                        await ListAll();
                        break;
                    case '1':
                        await CreateNew();
                        break;
                    case '2':
                        await UpdateLast();
                        break;
                    case '3':
                        await DeleteLast();
                        break;
                    case '4':
                        await GetLast();
                        break;
                    default:
                        // nothing
                        break;
                }
            }
        }

        public abstract Task GetLast();
        public abstract Task DeleteLast();
        public abstract Task UpdateLast();
        public abstract Task CreateNew();
        public abstract Task ListAll();
    }
}
