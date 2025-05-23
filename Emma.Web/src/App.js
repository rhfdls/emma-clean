import React from "react";
import { BrowserRouter as Router, Routes, Route, Link } from "react-router-dom";
import { Container, AppBar, Toolbar, Button, Box, Typography } from "@mui/material";
import EmmaDemo from "./components/EmmaDemo";

// Create a simple Home component
function Home() {
  return (
    <Box sx={{ mt: 4, textAlign: 'center' }}>
      <Typography variant="h3" component="h1" gutterBottom>
        Welcome to EMMA
      </Typography>
      <Typography variant="h5" component="h2" gutterBottom>
        Enhanced Multi-Modal AI for Real Estate
      </Typography>
      <Typography variant="body1" sx={{ mt: 3, mb: 4 }}>
        EMMA helps real estate professionals analyze conversations and suggest next best actions.
      </Typography>
      <Button 
        variant="contained" 
        color="primary" 
        size="large" 
        component={Link} 
        to="/demo"
      >
        Try the Demo
      </Button>
    </Box>
  );
}

function App() {
  return (
    <Router>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            EMMA Demo
          </Typography>
          <Button color="inherit" component={Link} to="/">
            Home
          </Button>
          <Button color="inherit" component={Link} to="/demo">
            Try Demo
          </Button>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Routes>
          <Route path="/demo" element={<EmmaDemo />} />
          <Route path="/" element={<Home />} />
        </Routes>
      </Container>
    </Router>
  );
}

export default App;
