/* global React, ReactDOM */
const App = () => (
  <React.Fragment>
    <Header />
    <Hero />
    <WhatItIs />
    <HowItWorks />
    <UseCases />
    <Technology />
    <CTA />
    <Footer />
  </React.Fragment>
);

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
