﻿namespace Castanha.Application.UseCases.Deposit
{
    using System.Threading.Tasks;
    using Castanha.Application.Outputs;
    using Castanha.Domain.ValueObjects;
    using Castanha.Application.ServiceBus;
    using Castanha.Application.Repositories;
    using Castanha.Domain.Accounts;

    public class DepositInteractor : IInputBoundary<DepositInput>
    {
        private readonly IPublisher bus;
        private readonly IAccountReadOnlyRepository accountReadOnlyRepository;
        private readonly IOutputBoundary<DepositOutput> outputBoundary;
        private readonly IOutputConverter responseConverter;

        public DepositInteractor(
            IAccountReadOnlyRepository accountReadOnlyRepository,
            IPublisher bus,
            IOutputBoundary<DepositOutput> outputBoundary,
            IOutputConverter responseConverter)
        {
            this.accountReadOnlyRepository = accountReadOnlyRepository;
            this.bus = bus;
            this.outputBoundary = outputBoundary;
            this.responseConverter = responseConverter;
        }

        public async Task Process(DepositInput input)
        {
            Account account = await accountReadOnlyRepository.Get(input.AccountId);
            if (account == null)
                throw new AccountNotFoundException($"The account {input.AccountId} does not exists or is already closed.");

            Credit credit = new Credit(account.Id, input.Amount);
            account.Deposit(credit);

            var domainEvents = account.GetEvents();
            await bus.Publish(domainEvents);

            TransactionOutput transactionResponse = responseConverter.Map<TransactionOutput>(credit);
            DepositOutput response = new DepositOutput(transactionResponse, account.GetCurrentBalance().Value);

            outputBoundary.Populate(response);
        }
    }
}
