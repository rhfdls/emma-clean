import React, { useEffect, useState } from "react";
import axios from "axios";
import {
  Container,
  Typography,
  Box,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Button,
  TextField,
  Paper,
  Grid,
  Snackbar,
  Alert
} from "@mui/material";

const API_BASE = "http://localhost:5000/api/dataentry"; // Update if backend runs on different port

function App() {
  const [messages, setMessages] = useState([]);
  const [organizations, setOrganizations] = useState([]);
  const [agents, setAgents] = useState([]);
  const [selectedOrg, setSelectedOrg] = useState("");
  const [selectedAgent, setSelectedAgent] = useState("");
  const [clientFirstName, setClientFirstName] = useState("");
  const [clientLastName, setClientLastName] = useState("");
  const [messageType, setMessageType] = useState("Text");
  const [content, setContent] = useState("");
  const [occurredAt, setOccurredAt] = useState("");
  const [snackbar, setSnackbar] = useState({ open: false, message: "", severity: "success" });

  useEffect(() => {
    axios.get(`${API_BASE}/organizations`).then(res => setOrganizations(res.data));
  }, []);

  useEffect(() => {
    if (selectedOrg) {
      axios.get(`${API_BASE}/agents/${selectedOrg}`).then(res => setAgents(res.data));
    } else {
      setAgents([]);
      setSelectedAgent("");
      setMessages([]);
    }
  }, [selectedOrg]);

  useEffect(() => {
    if (selectedOrg && selectedAgent) {
      fetchMessages();
    } else {
      setMessages([]);
    }
    // eslint-disable-next-line
  }, [selectedAgent]);

  const fetchMessages = async () => {
    if (!selectedOrg || !selectedAgent) return;
    try {
      const res = await axios.get(`${API_BASE}/messages`, {
        params: { organizationId: selectedOrg, agentId: selectedAgent, count: 10 }
      });
      setMessages(res.data);
    } catch {
      setMessages([]);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedOrg || !selectedAgent || !clientFirstName || !clientLastName || !content) {
      setSnackbar({ open: true, message: "Please fill in all required fields.", severity: "error" });
      return;
    }
    try {
      await axios.post(`${API_BASE}/add-message`, {
        organizationId: selectedOrg,
        agentId: selectedAgent,
        clientFirstName,
        clientLastName,
        content,
        messageType,
        occurredAt: occurredAt || null,
        newConversation: true
      });
      setSnackbar({ open: true, message: "Message added successfully!", severity: "success" });
      setContent("");
      setOccurredAt("");
      fetchMessages();
    } catch (err) {
      setSnackbar({ open: true, message: err.response?.data || "Error adding message.", severity: "error" });
    }
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4 }}>
      <Grid container spacing={4}>
        {/* Data Entry Panel */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 4 }}>
            <Typography variant="h4" gutterBottom>
              Emma Data Entry
            </Typography>
            <Box component="form" onSubmit={handleSubmit}>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth required>
                    <InputLabel>Organization</InputLabel>
                    <Select
                      value={selectedOrg}
                      label="Organization"
                      onChange={e => setSelectedOrg(e.target.value)}
                    >
                      {organizations.map(org => (
                        <MenuItem key={org.id} value={org.id}>{org.email || org.name || org.id}</MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth required>
                    <InputLabel>Agent</InputLabel>
                    <Select
                      value={selectedAgent}
                      label="Agent"
                      onChange={e => setSelectedAgent(e.target.value)}
                      disabled={!selectedOrg}
                    >
                      {agents.map(agent => (
                        <MenuItem key={agent.id} value={agent.id}>{agent.firstName} {agent.lastName}</MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    label="Client First Name"
                    value={clientFirstName}
                    onChange={e => setClientFirstName(e.target.value)}
                    required
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    label="Client Last Name"
                    value={clientLastName}
                    onChange={e => setClientLastName(e.target.value)}
                    required
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth>
                    <InputLabel>Message Type</InputLabel>
                    <Select
                      value={messageType}
                      label="Message Type"
                      onChange={e => setMessageType(e.target.value)}
                    >
                      <MenuItem value="Text">Text Message</MenuItem>
                      <MenuItem value="Email">Email</MenuItem>
                      <MenuItem value="Note">Note</MenuItem>
                      <MenuItem value="Call">Call (Transcript)</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    label="Occurred At (optional)"
                    type="datetime-local"
                    value={occurredAt}
                    onChange={e => setOccurredAt(e.target.value)}
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Content"
                    value={content}
                    onChange={e => setContent(e.target.value)}
                    required
                    fullWidth
                    multiline
                    minRows={4}
                  />
                </Grid>
                <Grid item xs={12}>
                  <Button type="submit" variant="contained" color="primary" fullWidth>
                    Add Message
                  </Button>
                </Grid>
              </Grid>
            </Box>
          </Paper>
        </Grid>
        {/* Data Viewing Panel */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 4, minHeight: 300 }}>
            <Typography variant="h5" gutterBottom>
              Recent Messages
            </Typography>
            {messages.length > 0 ? (
              <Box sx={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr>
                      <th style={{ borderBottom: '1px solid #ccc', padding: 8 }}>Occurred At</th>
                      <th style={{ borderBottom: '1px solid #ccc', padding: 8 }}>Type</th>
                      <th style={{ borderBottom: '1px solid #ccc', padding: 8 }}>Client</th>
                      <th style={{ borderBottom: '1px solid #ccc', padding: 8 }}>Content</th>
                    </tr>
                  </thead>
                  <tbody>
                    {messages.map(msg => (
                      <tr key={msg.id}>
                        <td style={{ borderBottom: '1px solid #eee', padding: 8 }}>{new Date(msg.occurredAt).toLocaleString()}</td>
                        <td style={{ borderBottom: '1px solid #eee', padding: 8 }}>{msg.type}</td>
                        <td style={{ borderBottom: '1px solid #eee', padding: 8 }}>{msg.clientFirstName} {msg.clientLastName}</td>
                        <td style={{ borderBottom: '1px solid #eee', padding: 8, whiteSpace: 'pre-wrap' }}>{msg.payload}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </Box>
            ) : (
              <Typography variant="body2" color="text.secondary">No messages to display.</Typography>
            )}
          </Paper>
        </Grid>
      </Grid>
      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
      >
        <Alert severity={snackbar.severity} sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  );
}

export default App;
